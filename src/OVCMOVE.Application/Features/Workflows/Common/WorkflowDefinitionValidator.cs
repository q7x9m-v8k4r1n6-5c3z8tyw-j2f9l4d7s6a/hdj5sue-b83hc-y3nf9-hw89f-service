using System.Text.Json;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Workflows.Common;

public sealed class WorkflowDefinitionValidator
{
    private static readonly HashSet<string> AllowedNodeTypes =
    [
        WorkflowConstants.NodeType.TriggerActivated,
        WorkflowConstants.NodeType.TriggerAttacked,
        WorkflowConstants.NodeType.Condition,
        WorkflowConstants.NodeType.CreateVariable,
        WorkflowConstants.NodeType.SetVariable,
        WorkflowConstants.NodeType.RandomNumber,
        WorkflowConstants.NodeType.ReadInputValue,
        WorkflowConstants.NodeType.AdjustScore,
        WorkflowConstants.NodeType.Attack,
        WorkflowConstants.NodeType.SendMessage,
        WorkflowConstants.NodeType.ApplyCardEffect,
        WorkflowConstants.NodeType.Scope,
        WorkflowConstants.NodeType.Stop
    ];

    private static readonly HashSet<string> ConditionOperators =
    ["equals", "not_equals", "greater_than", "greater_or_equal", "less_than", "less_or_equal", "contains", "is_empty"];

    public void Validate(
        WorkflowDefinitionModel definition,
        string triggerType,
        bool requirePublishable,
        IReadOnlySet<string>? cardInputKeys = null)
    {
        if (definition.SchemaVersion != 1)
            throw Invalid("Chỉ hỗ trợ schemaVersion = 1.");
        if (definition.Nodes.Count is 0 or > 50)
            throw Invalid("Workflow phải có từ 1 đến 50 node.");
        if (definition.Edges.Count > 100)
            throw Invalid("Workflow không được vượt quá 100 liên kết.");

        var nodeById = new Dictionary<string, WorkflowNodeModel>(StringComparer.Ordinal);
        foreach (var node in definition.Nodes)
        {
            var nodeId = RequireText(node.Id, "Mỗi node phải có id.");
            if (!nodeById.TryAdd(nodeId, node))
                throw Invalid("Node id không được trùng nhau.");
        }

        foreach (var node in definition.Nodes)
        {
            if (!AllowedNodeTypes.Contains(node.Type))
                throw Invalid($"Loại node '{node.Type}' không được hỗ trợ.");
            ValidateNodeConfig(node, cardInputKeys);
        }

        var expectedTriggerNodeType = triggerType switch
        {
            WorkflowConstants.Trigger.Activated => WorkflowConstants.NodeType.TriggerActivated,
            WorkflowConstants.Trigger.Attacked => WorkflowConstants.NodeType.TriggerAttacked,
            _ => throw Invalid("Trigger chỉ có thể là 'activated' hoặc 'attacked'.")
        };
        var triggerNodes = definition.Nodes
            .Where(node => node.Type.StartsWith("trigger.", StringComparison.Ordinal))
            .ToArray();
        if (triggerNodes.Length != 1 || triggerNodes[0].Type != expectedTriggerNodeType)
            throw Invalid("Workflow phải có đúng một trigger khớp với loại đã chọn.");

        var edgeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in definition.Edges)
        {
            if (!edgeIds.Add(RequireText(edge.Id, "Mỗi liên kết phải có id.")))
                throw Invalid("Liên kết không được trùng id.");
            if (!nodeById.ContainsKey(edge.Source) || !nodeById.ContainsKey(edge.Target))
                throw Invalid("Liên kết đang trỏ tới node không tồn tại.");
            if (edge.Source == edge.Target)
                throw Invalid("Node không thể tự nối với chính nó.");
        }

        EnsureAcyclic(definition, nodeById.Keys);
        if (!requirePublishable) return;

        var reachable = CollectReachable(triggerNodes[0].Id, definition.Edges);
        if (definition.Nodes.Count < 2 || reachable.Count != definition.Nodes.Count)
            throw Invalid("Để xuất bản, mọi node phải nối với trigger và workflow phải có ít nhất một action.");

        foreach (var node in definition.Nodes)
        {
            var outgoing = definition.Edges.Where(edge => edge.Source == node.Id).ToArray();
            if (node.Type == WorkflowConstants.NodeType.Condition)
            {
                if (outgoing.Length != 2 ||
                    outgoing.Count(edge => edge.SourceHandle == "true") != 1 ||
                    outgoing.Count(edge => edge.SourceHandle == "false") != 1)
                    throw Invalid("Node điều kiện phải có đủ nhánh Đúng và Sai.");
            }
            else if (node.Type == WorkflowConstants.NodeType.Scope)
            {
                if (outgoing.Length != 2 ||
                    outgoing.Count(edge => edge.SourceHandle == "try") != 1 ||
                    outgoing.Count(edge => edge.SourceHandle == "catch") != 1)
                    throw Invalid("Node scope phải có đủ nhánh Try và Catch.");
            }
            else if (node.Type == WorkflowConstants.NodeType.Stop && outgoing.Length > 0)
            {
                throw Invalid("Node dừng không được nối tới bước tiếp theo.");
            }
            else if (outgoing.Length > 1)
            {
                throw Invalid("Action thường chỉ được nối tới tối đa một node tiếp theo.");
            }
        }
    }

    private static void ValidateNodeConfig(
        WorkflowNodeModel node,
        IReadOnlySet<string>? cardInputKeys)
    {
        if (node.Type.StartsWith("trigger.", StringComparison.Ordinal) ||
            node.Type == WorkflowConstants.NodeType.Scope ||
            node.Type == WorkflowConstants.NodeType.Stop)
            return;
        if (node.Config.ValueKind != JsonValueKind.Object)
            throw Invalid($"Node '{node.Id}' cần config dạng object.");

        switch (node.Type)
        {
            case WorkflowConstants.NodeType.Condition:
                var op = GetRequiredString(node.Config, "operator", node.Id);
                if (!ConditionOperators.Contains(op))
                    throw Invalid($"Toán tử '{op}' không được hỗ trợ.");
                RequireProperty(node.Config, "left", node.Id);
                if (op != "is_empty") RequireProperty(node.Config, "right", node.Id);
                break;
            case WorkflowConstants.NodeType.CreateVariable:
            case WorkflowConstants.NodeType.SetVariable:
                RequireMaxLength(GetRequiredString(node.Config, "name", node.Id), 100, node.Id, "name");
                RequireProperty(node.Config, "value", node.Id);
                break;
            case WorkflowConstants.NodeType.RandomNumber:
                RequireMaxLength(GetRequiredString(node.Config, "name", node.Id), 100, node.Id, "name");
                var min = RequireInteger(node.Config, "min", node.Id);
                var max = RequireInteger(node.Config, "max", node.Id);
                if (max == int.MaxValue || max < min || (long)max - min > 1_000_000)
                    throw Invalid($"Khoảng random của node '{node.Id}' không hợp lệ hoặc quá lớn.");
                break;
            case WorkflowConstants.NodeType.ReadInputValue:
                var inputKey = GetRequiredString(node.Config, "inputKey", node.Id);
                RequireMaxLength(inputKey, 100, node.Id, "inputKey");
                RequireMaxLength(GetRequiredString(node.Config, "variableName", node.Id), 100, node.Id, "variableName");
                if (cardInputKeys is not null && !cardInputKeys.Contains(inputKey))
                    throw Invalid(
                        $"Node '{node.Id}' tham chiếu input '{inputKey}' không tồn tại trong thẻ.");
                break;
            case WorkflowConstants.NodeType.AdjustScore:
                ValidateTarget(node.Config, node.Id, false);
                var delta = RequireInteger(node.Config, "delta", node.Id);
                if (delta is <= 0 or > 1_000_000)
                    throw Invalid($"Điểm cộng của node '{node.Id}' phải từ 1 đến 1.000.000.");
                RequireMaxLength(GetRequiredString(node.Config, "reason", node.Id), 500, node.Id, "reason");
                break;
            case WorkflowConstants.NodeType.Attack:
                var subAction = GetRequiredString(node.Config, "subAction", node.Id);
                if (subAction is not ("subtract" or "freeze" or "steal" or "transfer"))
                    throw Invalid($"Sub-action tấn công của node '{node.Id}' không hợp lệ.");
                if (subAction == "freeze")
                {
                    var durationSeconds = RequireInteger(node.Config, "durationSeconds", node.Id);
                    if (durationSeconds is <= 0 or > 604800)
                        throw Invalid($"Thời gian đóng băng của node '{node.Id}' phải từ 1 đến 604800 giây.");
                }
                else
                {
                    var amount = RequireInteger(node.Config, "amount", node.Id);
                    if (amount is <= 0 or > 1_000_000)
                        throw Invalid($"Số điểm tấn công của node '{node.Id}' phải từ 1 đến 1.000.000.");
                }
                ValidateDefenseTags(node.Config, node.Id);
                break;
            case WorkflowConstants.NodeType.SendMessage:
                ValidateTarget(node.Config, node.Id, true);
                RequireMaxLength(GetRequiredString(node.Config, "message", node.Id), 2000, node.Id, "message");
                break;
            case WorkflowConstants.NodeType.ApplyCardEffect:
                ValidateTarget(node.Config, node.Id, false);
                RequireMaxLength(GetRequiredString(node.Config, "effectKey", node.Id), 100, node.Id, "effectKey");
                if (node.Config.TryGetProperty("durationSeconds", out _))
                {
                    var duration = RequireInteger(node.Config, "durationSeconds", node.Id);
                    if (duration is < 0 or > 604800)
                        throw Invalid($"Thời lượng effect của node '{node.Id}' phải từ 0 đến 604800 giây.");
                }
                break;
        }
    }

    private static void ValidateTarget(JsonElement config, string nodeId, bool allowAll)
    {
        var target = GetRequiredString(config, "target", nodeId);
        if (target is "actor" or "target" || allowAll && target == "all-teams") return;
        if (target == "custom")
        {
            if (config.TryGetProperty("teamIds", out var teamIds) && teamIds.ValueKind == JsonValueKind.Array)
            {
                var values = teamIds.EnumerateArray().ToArray();
                if (values is { Length: > 0 and <= 100 } && values.All(item =>
                    item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out _))) return;
            }
            if (config.TryGetProperty("teamId", out var teamId) &&
                teamId.ValueKind == JsonValueKind.String &&
                Guid.TryParse(teamId.GetString(), out _)) return;
        }
        throw Invalid($"Target của node '{nodeId}' không hợp lệ.");
    }

    private static void ValidateDefenseTags(JsonElement config, string nodeId)
    {
        if (!config.TryGetProperty("defenseTags", out var tags) || tags.ValueKind != JsonValueKind.Array)
            throw Invalid($"Node '{nodeId}' cần danh sách tag thẻ phòng thủ.");
        var values = tags.EnumerateArray().ToArray();
        if (values.Length > 50 || values.Any(tag =>
            tag.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(tag.GetString()) ||
            tag.GetString()!.Length > 100))
            throw Invalid($"Tag thẻ phòng thủ của node '{nodeId}' không hợp lệ.");
    }

    private static string GetRequiredString(JsonElement config, string name, string nodeId)
    {
        if (!config.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw Invalid($"Node '{nodeId}' thiếu config '{name}'.");
        return value.GetString()!;
    }

    private static void RequireProperty(JsonElement config, string name, string nodeId)
    {
        if (!config.TryGetProperty(name, out _))
            throw Invalid($"Node '{nodeId}' thiếu config '{name}'.");
    }

    private static int RequireInteger(JsonElement config, string name, string nodeId)
    {
        if (!config.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
            throw Invalid($"Node '{nodeId}' cần '{name}' là số nguyên 32-bit.");
        return result;
    }

    private static void RequireMaxLength(
        string value,
        int maxLength,
        string nodeId,
        string field)
    {
        if (value.Length > maxLength)
            throw Invalid($"Config '{field}' của node '{nodeId}' không được vượt quá {maxLength} ký tự.");
    }

    private static string RequireText(string value, string error) =>
        string.IsNullOrWhiteSpace(value) ? throw Invalid(error) : value.Trim();

    private static void EnsureAcyclic(
        WorkflowDefinitionModel definition,
        IEnumerable<string> nodeIds)
    {
        var adjacency = definition.Edges
            .GroupBy(edge => edge.Source)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.Target).ToArray());
        var state = nodeIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);

        bool Visit(string id)
        {
            state[id] = 1;
            foreach (var target in adjacency.GetValueOrDefault(id, []))
            {
                if (state[target] == 1 || state[target] == 0 && Visit(target)) return true;
            }
            state[id] = 2;
            return false;
        }

        if (state.Keys.Any(id => state[id] == 0 && Visit(id)))
            throw Invalid("Workflow không hỗ trợ vòng lặp.");
    }

    private static HashSet<string> CollectReachable(
        string triggerId,
        IReadOnlyCollection<WorkflowEdgeModel> edges)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal) { triggerId };
        var queue = new Queue<string>();
        queue.Enqueue(triggerId);
        while (queue.TryDequeue(out var current))
        {
            foreach (var target in edges.Where(edge => edge.Source == current).Select(edge => edge.Target))
                if (reachable.Add(target)) queue.Enqueue(target);
        }
        return reachable;
    }

    private static ApplicationValidationException Invalid(string message) => new(message);
}
