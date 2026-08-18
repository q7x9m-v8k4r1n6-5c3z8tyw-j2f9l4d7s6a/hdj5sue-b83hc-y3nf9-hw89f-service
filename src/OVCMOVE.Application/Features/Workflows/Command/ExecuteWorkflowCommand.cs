using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class ExecuteWorkflowCommand : AuditedRequest, IRequest<WorkflowExecutionResultModel>
{
    public Guid WorkflowId { get; init; }
    public bool IsSimulation { get; init; }
    public WorkflowExecutionInputModel Input { get; init; } = new();
}

public sealed class ExecuteWorkflowCommandHandler(
    IWorkflowRepository repository,
    WorkflowDefinitionValidator validator,
    WorkflowRuntime runtime)
    : IRequestHandler<ExecuteWorkflowCommand, WorkflowExecutionResultModel>
{
    public async Task<WorkflowExecutionResultModel> Handle(
        ExecuteWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        var workflow = await repository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy workflow.");
        if (!request.IsSimulation && workflow.Status != WorkflowConstants.Status.Published)
            throw new ApplicationConflictException("Chỉ workflow đã xuất bản mới được phép chạy thật.");
        if (!request.IsSimulation && string.IsNullOrWhiteSpace(request.Input.EventId))
            throw new ApplicationValidationException("EventId là bắt buộc khi chạy thật để chống thực thi trùng.");

        var definition = WorkflowJson.DeserializeDefinition(workflow.DefinitionJson);
        validator.Validate(definition, workflow.TriggerType, true);
        var actor = request.GetActorOrSystem();
        var now = DateTime.UtcNow;
        var run = new WorkflowRun
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            RaceId = workflow.RaceId,
            CardKey = workflow.CardKey,
            TriggerType = workflow.TriggerType,
            EventId = string.IsNullOrWhiteSpace(request.Input.EventId) ? null : request.Input.EventId.Trim(),
            Status = "running",
            IsSimulation = request.IsSimulation,
            InputJson = JsonSerializer.Serialize(request.Input, WorkflowJson.Options),
            OutputJson = "{}",
            StartedAt = now,
            CreatedBy = actor,
            CreatedAt = now,
            ModifiedBy = actor,
            ModifiedAt = now
        };

        await repository.CreateRunAsync(run, cancellationToken);

        try
        {
            var outcome = await runtime.ExecuteAsync(
                workflow,
                definition,
                request.Input,
                request.IsSimulation,
                cancellationToken);
            var result = new WorkflowExecutionResultModel(
                run.Id,
                "succeeded",
                request.IsSimulation,
                outcome.Trace,
                outcome.Effects,
                outcome.Variables);
            run.Status = result.Status;
            run.OutputJson = JsonSerializer.Serialize(result, WorkflowJson.Options);
            run.CompletedAt = DateTime.UtcNow;
            run.ModifiedAt = run.CompletedAt.Value;
            await repository.CompleteRunAsync(run, CancellationToken.None);
            return result;
        }
        catch (OperationCanceledException)
        {
            run.Status = "canceled";
            run.Error = "Workflow execution was cancelled.";
            run.CompletedAt = DateTime.UtcNow;
            run.ModifiedAt = run.CompletedAt.Value;
            await repository.CompleteRunAsync(run, CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            run.Status = "failed";
            run.Error = exception.Message.Length > 2000
                ? exception.Message[..2000]
                : exception.Message;
            run.CompletedAt = DateTime.UtcNow;
            run.ModifiedAt = run.CompletedAt.Value;
            await repository.CompleteRunAsync(run, CancellationToken.None);
            throw;
        }
    }
}

public sealed record WorkflowRuntimeOutcome(
    IReadOnlyCollection<WorkflowTraceItemModel> Trace,
    IReadOnlyCollection<WorkflowEffectModel> Effects,
    IReadOnlyDictionary<string, JsonElement> Variables);

public sealed partial class WorkflowRuntime(ISender sender, IWorkflowRepository repository)
{
    public async Task<WorkflowRuntimeOutcome> ExecuteAsync(
        Workflow workflow,
        WorkflowDefinitionModel definition,
        WorkflowExecutionInputModel input,
        bool isSimulation,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, JsonElement>(input.Variables, StringComparer.OrdinalIgnoreCase);
        var trace = new List<WorkflowTraceItemModel>();
        var effects = new List<WorkflowEffectModel>();
        var nodes = definition.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var current = definition.Nodes.Single(node => node.Type.StartsWith("trigger.", StringComparison.Ordinal));
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
                    var scopeEdges = definition.Edges.Where(edge => edge.Source == current.Id).ToArray();
                    var tryEdge = scopeEdges.Single(edge => edge.SourceHandle == "try");
                    var catchEdge = scopeEdges.Single(edge => edge.SourceHandle == "catch");
                    catchTargets.Push(catchEdge.Target);
                    trace.Add(new WorkflowTraceItemModel(
                        current.Id, current.Type, "succeeded", "Đã bắt đầu nhánh Try."));
                    current = nodes[tryEdge.Target];
                    continue;
                }

                string? branch = null;
                var detail = current.Type.StartsWith("trigger.", StringComparison.Ordinal)
                    ? "Đã nhận sự kiện kích hoạt."
                    : await ExecuteNodeAsync(
                        current, workflow, input, variables, effects,
                        isSimulation, cancellationToken);
                if (current.Type == WorkflowConstants.NodeType.Condition)
                {
                    branch = EvaluateCondition(current.Config, workflow, input, variables)
                        ? "true"
                        : "false";
                    detail = branch == "true" ? "Điều kiện đúng." : "Điều kiện sai.";
                }
                trace.Add(new WorkflowTraceItemModel(current.Id, current.Type, "succeeded", detail));
                if (current.Type == WorkflowConstants.NodeType.Stop) break;

                var outgoing = definition.Edges.Where(edge => edge.Source == current.Id);
                if (branch is not null) outgoing = outgoing.Where(edge => edge.SourceHandle == branch);
                var nextEdge = outgoing.FirstOrDefault();
                current = nextEdge is null ? null : nodes[nextEdge.Target];
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException && catchTargets.Count > 0)
            {
                trace.Add(new WorkflowTraceItemModel(
                    current!.Id, current.Type, "failed", exception.Message));
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
                var value = ResolveOperand(node.Config.GetProperty("value"), workflow, input, variables);
                variables[name] = value;
                return $"Đã gán biến {name}.";
            }
            case WorkflowConstants.NodeType.RandomNumber:
            {
                var name = node.Config.GetProperty("name").GetString()!;
                var min = node.Config.GetProperty("min").GetInt32();
                var max = node.Config.GetProperty("max").GetInt32();
                if (max < min) throw new ApplicationValidationException("Giá trị max phải lớn hơn hoặc bằng min.");
                var value = RandomNumberGenerator.GetInt32(min, checked(max + 1));
                variables[name] = WorkflowJson.ToElement(value);
                return $"Đã sinh {name} = {value}.";
            }
            case WorkflowConstants.NodeType.ReadInputValue:
            {
                var inputKey = node.Config.GetProperty("inputKey").GetString()!;
                var variableName = node.Config.GetProperty("variableName").GetString()!;
                variables[variableName] = ResolvePath($"payload.inputs.{inputKey}", workflow, input, variables);
                return $"Đã đọc input {inputKey} vào biến {variableName}.";
            }
            case WorkflowConstants.NodeType.AdjustScore:
            {
                var target = node.Config.GetProperty("target").GetString()!;
                var teamIds = ResolveTeamIds(target, input, node.Config);
                var delta = node.Config.GetProperty("delta").GetInt32();
                var reason = RenderTemplate(node.Config.GetProperty("reason").GetString()!, workflow, input, variables);
                if (delta == 0) throw new ApplicationValidationException("Điểm thay đổi phải khác 0.");
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
                        if (scoreResult is null) throw new ApplicationNotFoundException("Không tìm thấy đội trong race.");
                    }
                    var data = WorkflowJson.ToElement(new { teamId, delta, reason });
                    effects.Add(new WorkflowEffectModel("team.adjust_score", target, data, !isSimulation));
                }
                return $"{(delta > 0 ? "Cộng" : "Trừ")} {Math.Abs(delta)} điểm cho {teamIds.Count} đội.";
            }
            case WorkflowConstants.NodeType.Attack:
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
                var amount = subAction == "freeze" ? 0 : node.Config.GetProperty("amount").GetInt32();
                var durationSeconds = subAction == "freeze"
                    ? node.Config.GetProperty("durationSeconds").GetInt32()
                    : 0;
                var activatedDefenseWorkflows = new List<string>();
                if (defenseTags.Length > 0)
                {
                    var defenseWorkflows = (await repository.GetByRaceAsync(
                        workflow.RaceId, null, cancellationToken))
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
                        if (result is null) throw new ApplicationNotFoundException("Không tìm thấy đội trong race.");
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
            case WorkflowConstants.NodeType.SendMessage:
            {
                var target = node.Config.GetProperty("target").GetString()!;
                var message = RenderTemplate(node.Config.GetProperty("message").GetString()!, workflow, input, variables);
                var recipients = BuildRecipients(target, input, node.Config);
                if (!isSimulation)
                {
                    var messageResult = await sender.Send(new SendRaceMessageCommand
                    {
                        RaceId = workflow.RaceId,
                        Recipients = recipients,
                        Body = message
                    }, cancellationToken);
                    if (messageResult is null) throw new ApplicationNotFoundException("Không tìm thấy race.");
                }
                effects.Add(new WorkflowEffectModel(
                    "notify.send_message", target,
                    WorkflowJson.ToElement(new { message }), !isSimulation));
                return $"Đã tạo thông báo cho {target}.";
            }
            case WorkflowConstants.NodeType.ApplyCardEffect:
            {
                var target = node.Config.GetProperty("target").GetString()!;
                _ = ResolveTeamIds(target, input, node.Config).First();
                effects.Add(new WorkflowEffectModel(
                    "card.apply_effect", target, node.Config.Clone(), !isSimulation));
                return "Đã phát sinh card effect cho backend card xử lý.";
            }
            case WorkflowConstants.NodeType.Stop:
                return "Workflow đã dừng.";
            default:
                throw new ApplicationValidationException($"Không hỗ trợ node '{node.Type}'.");
        }
    }

    private static IReadOnlyCollection<Guid> ResolveTeamIds(
        string target,
        WorkflowExecutionInputModel input,
        JsonElement config)
    {
        if (target == "actor" && input.ActorTeamId.HasValue) return [input.ActorTeamId.Value];
        if (target == "target" && input.TargetTeamId.HasValue) return [input.TargetTeamId.Value];
        if (target == "custom")
        {
            if (config.TryGetProperty("teamIds", out var teamIds) && teamIds.ValueKind == JsonValueKind.Array)
            {
                var result = teamIds.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out _))
                    .Select(item => Guid.Parse(item.GetString()!))
                    .Distinct()
                    .ToArray();
                if (result.Length > 0) return result;
            }
            if (config.TryGetProperty("teamId", out var teamId) &&
                teamId.ValueKind == JsonValueKind.String &&
                Guid.TryParse(teamId.GetString(), out var parsedTeamId)) return [parsedTeamId];
        }
        throw new ApplicationValidationException($"Sự kiện thiếu team cho target '{target}'.");
    }

    private static IReadOnlyCollection<RaceMessageRecipientModel> BuildRecipients(
        string target,
        WorkflowExecutionInputModel input,
        JsonElement config) => target switch
        {
            "all-teams" => [new RaceMessageRecipientModel { Key = "all-teams", Label = "Tất cả team", Type = "all-teams" }],
            "actor" or "target" or "custom" => ResolveTeamIds(target, input, config).Select(TeamRecipient).ToArray(),
            _ => throw new ApplicationValidationException("Đối tượng nhận thông báo không hợp lệ.")
        };

    private static RaceMessageRecipientModel TeamRecipient(Guid teamId) => new()
    {
        Key = $"team:{teamId:D}",
        Label = "Đội chơi",
        Type = "team"
    };

    private static bool EvaluateCondition(
        JsonElement config,
        Workflow workflow,
        WorkflowExecutionInputModel input,
        IReadOnlyDictionary<string, JsonElement> variables)
    {
        var left = ResolveOperand(config.GetProperty("left"), workflow, input, variables);
        var op = config.GetProperty("operator").GetString();
        if (op == "is_empty") return IsEmpty(left);
        var right = ResolveOperand(config.GetProperty("right"), workflow, input, variables);
        return op switch
        {
            "equals" => Compare(left, right) == 0,
            "not_equals" => Compare(left, right) != 0,
            "greater_than" => Compare(left, right) > 0,
            "greater_or_equal" => Compare(left, right) >= 0,
            "less_than" => Compare(left, right) < 0,
            "less_or_equal" => Compare(left, right) <= 0,
            "contains" => ElementToText(left).Contains(ElementToText(right), StringComparison.OrdinalIgnoreCase),
            _ => throw new ApplicationValidationException("Toán tử điều kiện không hợp lệ.")
        };
    }

    private static JsonElement ResolveOperand(
        JsonElement operand,
        Workflow workflow,
        WorkflowExecutionInputModel input,
        IReadOnlyDictionary<string, JsonElement> variables)
    {
        if (operand.ValueKind != JsonValueKind.Object ||
            !operand.TryGetProperty("kind", out var kind)) return operand.Clone();
        if (kind.GetString() == "literal")
            return operand.TryGetProperty("value", out var literal) ? literal.Clone() : WorkflowJson.ToElement<object?>(null);
        if (kind.GetString() != "path" || !operand.TryGetProperty("path", out var pathElement))
            throw new ApplicationValidationException("Operand phải là literal hoặc path.");
        return ResolvePath(pathElement.GetString() ?? string.Empty, workflow, input, variables);
    }

    private static JsonElement ResolvePath(
        string path,
        Workflow workflow,
        WorkflowExecutionInputModel input,
        IReadOnlyDictionary<string, JsonElement> variables)
    {
        if (path == "event.actorTeamId") return WorkflowJson.ToElement(input.ActorTeamId);
        if (path == "event.targetTeamId") return WorkflowJson.ToElement(input.TargetTeamId);
        if (path == "event.cardKey") return WorkflowJson.ToElement(workflow.CardKey);
        if (path == "event.triggerType") return WorkflowJson.ToElement(workflow.TriggerType);
        if (path.StartsWith("variables.", StringComparison.OrdinalIgnoreCase) &&
            variables.TryGetValue(path[10..], out var variable)) return variable.Clone();
        if (path.StartsWith("payload.", StringComparison.OrdinalIgnoreCase))
        {
            var current = input.Payload;
            foreach (var segment in path[8..].Split('.', StringSplitOptions.RemoveEmptyEntries))
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var next)) current = next;
                else return WorkflowJson.ToElement<object?>(null);
            return current.Clone();
        }
        return WorkflowJson.ToElement<object?>(null);
    }

    private static int Compare(JsonElement left, JsonElement right)
    {
        if (TryDecimal(left, out var leftNumber) && TryDecimal(right, out var rightNumber))
            return leftNumber.CompareTo(rightNumber);
        return string.Compare(ElementToText(left), ElementToText(right), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryDecimal(JsonElement element, out decimal value) =>
        element.ValueKind == JsonValueKind.Number
            ? element.TryGetDecimal(out value)
            : decimal.TryParse(ElementToText(element), NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private static bool IsEmpty(JsonElement value) => value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ||
        value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(value.GetString()) ||
        value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0 ||
        value.ValueKind == JsonValueKind.Object && !value.EnumerateObject().Any();

    private static string ElementToText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => element.GetRawText()
    };

    private static string RenderTemplate(
        string template,
        Workflow workflow,
        WorkflowExecutionInputModel input,
        IReadOnlyDictionary<string, JsonElement> variables) =>
        TemplateTokenRegex().Replace(template, match => ElementToText(
            ResolvePath(match.Groups[1].Value.Trim(), workflow, input, variables)));

    [GeneratedRegex(@"\{\{\s*([^{}]+?)\s*\}\}")]
    private static partial Regex TemplateTokenRegex();
}
