using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed record WorkflowRuntimeOutcome(
    IReadOnlyCollection<WorkflowTraceItemModel> Trace,
    IReadOnlyCollection<WorkflowEffectModel> Effects,
    IReadOnlyDictionary<string, JsonElement> Variables);

public sealed class WorkflowRuntime(ISender sender, IWorkflowRepository repository)
{
    public async Task<WorkflowRuntimeOutcome> ExecuteAsync(
        Workflow workflow,
        WorkflowDefinitionModel definition,
        WorkflowExecutionInputModel input,
        bool isSimulation,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, JsonElement>(
            input.Variables,
            StringComparer.OrdinalIgnoreCase);
        var trace = new List<WorkflowTraceItemModel>();
        var effects = new List<WorkflowEffectModel>();
        var nodes = definition.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var current = definition.Nodes.Single(
            node => node.Type.StartsWith("trigger.", StringComparison.Ordinal));
        var catchTargets = new Stack<string>();
        var stepCount = 0;

        while (current is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++stepCount > 50)
                throw new ApplicationValidationException("Workflow vượt quá giới hạn 50 bước.");

            try
            {
                if (current.Type == WorkflowConstants.NodeType.Scope)
                {
                    var scopeEdges = definition.Edges
                        .Where(edge => edge.Source == current.Id)
                        .ToArray();
                    var tryEdge = scopeEdges.Single(edge => edge.SourceHandle == "try");
                    var catchEdge = scopeEdges.Single(edge => edge.SourceHandle == "catch");
                    catchTargets.Push(catchEdge.Target);
                    trace.Add(new WorkflowTraceItemModel(
                        current.Id,
                        current.Type,
                        "succeeded",
                        "Đã bắt đầu nhánh Try."));
                    current = nodes[tryEdge.Target];
                    continue;
                }

                string? branch = null;
                var detail = current.Type.StartsWith("trigger.", StringComparison.Ordinal)
                    ? "Đã nhận sự kiện kích hoạt."
                    : await ExecuteNodeAsync(
                        current,
                        workflow,
                        input,
                        variables,
                        effects,
                        isSimulation,
                        cancellationToken);
                if (current.Type == WorkflowConstants.NodeType.Condition)
                {
                    branch = WorkflowExpressionEvaluator.EvaluateCondition(
                        current.Config,
                        workflow,
                        input,
                        variables)
                        ? "true"
                        : "false";
                    detail = branch == "true" ? "Điều kiện đúng." : "Điều kiện sai.";
                }
                trace.Add(new WorkflowTraceItemModel(
                    current.Id,
                    current.Type,
                    "succeeded",
                    detail));
                if (current.Type == WorkflowConstants.NodeType.Stop) break;

                var outgoing = definition.Edges.Where(edge => edge.Source == current.Id);
                if (branch is not null)
                    outgoing = outgoing.Where(edge => edge.SourceHandle == branch);
                var nextEdge = outgoing.FirstOrDefault();
                current = nextEdge is null ? null : nodes[nextEdge.Target];
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException && catchTargets.Count > 0)
            {
                trace.Add(new WorkflowTraceItemModel(
                    current!.Id,
                    current.Type,
                    "failed",
                    exception.Message));
                current = nodes[catchTargets.Pop()];
            }
        }

        return new WorkflowRuntimeOutcome(trace, effects, variables);
    }

    private async Task<string> ExecuteNodeAsync(
        WorkflowNodeModel node,
        Workflow workflow,
        WorkflowExecutionInputModel input,
        Dictionary<string, JsonElement> variables,
        ICollection<WorkflowEffectModel> effects,
        bool isSimulation,
        CancellationToken cancellationToken)
    {
        switch (node.Type)
        {
            case WorkflowConstants.NodeType.Condition:
                return "Đang đánh giá điều kiện.";
            case WorkflowConstants.NodeType.CreateVariable:
            case WorkflowConstants.NodeType.SetVariable:
            {
                var name = node.Config.GetProperty("name").GetString()!;
                var value = WorkflowExpressionEvaluator.ResolveOperand(
                    node.Config.GetProperty("value"),
                    workflow,
                    input,
                    variables);
                variables[name] = value;
                return $"Đã gán biến {name}.";
            }
            case WorkflowConstants.NodeType.RandomNumber:
            {
                var name = node.Config.GetProperty("name").GetString()!;
                var min = node.Config.GetProperty("min").GetInt32();
                var max = node.Config.GetProperty("max").GetInt32();
                if (max < min)
                    throw new ApplicationValidationException("Giá trị max phải lớn hơn hoặc bằng min.");
                var value = RandomNumberGenerator.GetInt32(min, checked(max + 1));
                variables[name] = WorkflowJson.ToElement(value);
                return $"Đã sinh {name} = {value}.";
            }
            case WorkflowConstants.NodeType.ReadInputValue:
            {
                var inputKey = node.Config.GetProperty("inputKey").GetString()!;
                var variableName = node.Config.GetProperty("variableName").GetString()!;
                variables[variableName] = WorkflowExpressionEvaluator.ResolvePath(
                    $"payload.inputs.{inputKey}",
                    workflow,
                    input,
                    variables);
                return $"Đã đọc input {inputKey} vào biến {variableName}.";
            }
            case WorkflowConstants.NodeType.AdjustScore:
            {
                var target = node.Config.GetProperty("target").GetString()!;
                var teamIds = WorkflowTargetResolver.ResolveTeamIds(target, input, node.Config);
                var delta = node.Config.GetProperty("delta").GetInt32();
                var reason = WorkflowExpressionEvaluator.RenderTemplate(
                    node.Config.GetProperty("reason").GetString()!,
                    workflow,
                    input,
                    variables);
                if (delta == 0)
                    throw new ApplicationValidationException("Điểm thay đổi phải khác 0.");
                foreach (var teamId in teamIds)
                {
                    if (!isSimulation)
                    {
                        var scoreResult = await sender.Send(new UpdateTeamScoreCommand
                        {
                            RaceId = workflow.RaceId,
                            TeamId = teamId,
                            Delta = delta,
                            Reason = reason
                        }, cancellationToken);
                        if (scoreResult is null)
                            throw new ApplicationNotFoundException("Không tìm thấy đội trong race.");
                    }
                    var data = WorkflowJson.ToElement(new { teamId, delta, reason });
                    effects.Add(new WorkflowEffectModel(
                        "team.adjust_score",
                        target,
                        data,
                        !isSimulation));
                }
                return $"{(delta > 0 ? "Cộng" : "Trừ")} {Math.Abs(delta)} điểm cho {teamIds.Count} đội.";
            }
            case WorkflowConstants.NodeType.Attack:
                return await ExecuteAttackAsync(
                    node,
                    workflow,
                    input,
                    variables,
                    effects,
                    isSimulation,
                    cancellationToken);
            case WorkflowConstants.NodeType.SendMessage:
            {
                var target = node.Config.GetProperty("target").GetString()!;
                var message = WorkflowExpressionEvaluator.RenderTemplate(
                    node.Config.GetProperty("message").GetString()!,
                    workflow,
                    input,
                    variables);
                var recipients = WorkflowTargetResolver.BuildRecipients(target, input, node.Config);
                if (!isSimulation)
                {
                    var messageResult = await sender.Send(new SendRaceMessageCommand
                    {
                        RaceId = workflow.RaceId,
                        Recipients = recipients,
                        Body = message
                    }, cancellationToken);
                    if (messageResult is null)
                        throw new ApplicationNotFoundException("Không tìm thấy race.");
                }
                effects.Add(new WorkflowEffectModel(
                    "notify.send_message",
                    target,
                    WorkflowJson.ToElement(new { message }),
                    !isSimulation));
                return $"Đã tạo thông báo cho {target}.";
            }
            case WorkflowConstants.NodeType.ApplyCardEffect:
            {
                var target = node.Config.GetProperty("target").GetString()!;
                _ = WorkflowTargetResolver.ResolveTeamIds(target, input, node.Config).First();
                effects.Add(new WorkflowEffectModel(
                    "card.apply_effect",
                    target,
                    node.Config.Clone(),
                    !isSimulation));
                return "Đã phát sinh card effect cho backend card xử lý.";
            }
            case WorkflowConstants.NodeType.Stop:
                return "Workflow đã dừng.";
            default:
                throw new ApplicationValidationException($"Không hỗ trợ node '{node.Type}'.");
        }
    }

    private async Task<string> ExecuteAttackAsync(
        WorkflowNodeModel node,
        Workflow workflow,
        WorkflowExecutionInputModel input,
        Dictionary<string, JsonElement> variables,
        ICollection<WorkflowEffectModel> effects,
        bool isSimulation,
        CancellationToken cancellationToken)
    {
        var subAction = node.Config.GetProperty("subAction").GetString()!;
        var actorTeamId = input.ActorTeamId
            ?? throw new ApplicationValidationException("Action tấn công cần đội kích hoạt.");
        var targetTeamId = input.TargetTeamId
            ?? throw new ApplicationValidationException("Action tấn công cần đội mục tiêu.");
        var defenseTags = node.Config.GetProperty("defenseTags")
            .EnumerateArray()
            .Select(tag => tag.GetString()!)
            .ToArray();
        var amount = subAction == "freeze"
            ? 0
            : node.Config.GetProperty("amount").GetInt32();
        var durationSeconds = subAction == "freeze"
            ? node.Config.GetProperty("durationSeconds").GetInt32()
            : 0;
        var activatedDefenseWorkflows = new List<string>();
        if (defenseTags.Length > 0)
        {
            var defenseWorkflows = (await repository.GetByRaceAsync(
                workflow.RaceId,
                null,
                cancellationToken))
                .Where(item => item.Id != workflow.Id &&
                    item.Status == WorkflowConstants.Status.Published &&
                    item.TriggerType == WorkflowConstants.Trigger.Attacked &&
                    defenseTags.Any(tag =>
                        string.Equals(tag, item.CardKey, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(tag, item.CardName, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            foreach (var defenseWorkflow in defenseWorkflows)
            {
                var eventId = input.EventId is null
                    ? null
                    : $"def:{Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{input.EventId}:{defenseWorkflow.Id:D}"))).ToLowerInvariant()}";
                await sender.Send(new ExecuteWorkflowCommand
                {
                    WorkflowId = defenseWorkflow.Id,
                    IsSimulation = isSimulation,
                    Input = new WorkflowExecutionInputModel
                    {
                        EventId = eventId,
                        ActorTeamId = actorTeamId,
                        TargetTeamId = targetTeamId,
                        Variables = variables,
                        Payload = input.Payload
                    }
                }, cancellationToken);
                activatedDefenseWorkflows.Add(defenseWorkflow.CardKey);
            }
        }

        var scoreChanges = subAction switch
        {
            "subtract" => new[] { (TeamId: targetTeamId, Delta: -amount) },
            "steal" => new[]
            {
                (TeamId: targetTeamId, Delta: -amount),
                (TeamId: actorTeamId, Delta: amount)
            },
            "transfer" => new[]
            {
                (TeamId: actorTeamId, Delta: -amount),
                (TeamId: targetTeamId, Delta: amount)
            },
            "freeze" => Array.Empty<(Guid TeamId, int Delta)>(),
            _ => throw new ApplicationValidationException("Sub-action tấn công không hợp lệ.")
        };
        foreach (var change in scoreChanges)
        {
            if (!isSimulation)
            {
                var result = await sender.Send(new UpdateTeamScoreCommand
                {
                    RaceId = workflow.RaceId,
                    TeamId = change.TeamId,
                    Delta = change.Delta,
                    Reason = $"Tấn công: {subAction}"
                }, cancellationToken);
                if (result is null)
                    throw new ApplicationNotFoundException("Không tìm thấy đội trong race.");
            }
        }

        effects.Add(new WorkflowEffectModel(
            $"attack.{subAction}",
            "target",
            WorkflowJson.ToElement(new
            {
                actorTeamId,
                targetTeamId,
                amount,
                durationSeconds,
                defenseTags,
                activatedDefenseWorkflows,
                scoreChanges
            }),
            !isSimulation));
        return subAction == "freeze"
            ? $"Đóng băng đội mục tiêu trong {durationSeconds} giây."
            : $"Đã thực hiện tấn công {subAction} với {amount} điểm.";
    }
}
