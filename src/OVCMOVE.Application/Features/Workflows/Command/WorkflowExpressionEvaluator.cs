using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Workflows.Command;

internal static partial class WorkflowExpressionEvaluator
{
    public static bool EvaluateCondition(
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
            "contains" => ElementToText(left).Contains(
                ElementToText(right),
                StringComparison.OrdinalIgnoreCase),
            _ => throw new ApplicationValidationException("Toán tử điều kiện không hợp lệ.")
        };
    }

    public static JsonElement ResolveOperand(
        JsonElement operand,
        Workflow workflow,
        WorkflowExecutionInputModel input,
        IReadOnlyDictionary<string, JsonElement> variables)
    {
        if (operand.ValueKind != JsonValueKind.Object ||
            !operand.TryGetProperty("kind", out var kind))
            return operand.Clone();
        if (kind.GetString() == "literal")
            return operand.TryGetProperty("value", out var literal)
                ? literal.Clone()
                : WorkflowJson.ToElement<object?>(null);
        if (kind.GetString() != "path" || !operand.TryGetProperty("path", out var pathElement))
            throw new ApplicationValidationException("Operand phải là literal hoặc path.");
        return ResolvePath(pathElement.GetString() ?? string.Empty, workflow, input, variables);
    }

    public static JsonElement ResolvePath(
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
            variables.TryGetValue(path[10..], out var variable))
            return variable.Clone();
        if (path.StartsWith("payload.", StringComparison.OrdinalIgnoreCase))
        {
            var current = input.Payload;
            foreach (var segment in path[8..].Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.ValueKind == JsonValueKind.Object &&
                    current.TryGetProperty(segment, out var next))
                    current = next;
                else
                    return WorkflowJson.ToElement<object?>(null);
            }
            return current.Clone();
        }
        return WorkflowJson.ToElement<object?>(null);
    }

    public static string RenderTemplate(
        string template,
        Workflow workflow,
        WorkflowExecutionInputModel input,
        IReadOnlyDictionary<string, JsonElement> variables) =>
        TemplateTokenRegex().Replace(template, match => ElementToText(
            ResolvePath(match.Groups[1].Value.Trim(), workflow, input, variables)));

    private static int Compare(JsonElement left, JsonElement right)
    {
        if (IsBoolean(left) || IsBoolean(right))
        {
            if (!IsBoolean(left) || !IsBoolean(right))
                throw new ApplicationValidationException(
                    "Không thể so sánh boolean với kiểu dữ liệu khác.");
            return left.GetBoolean().CompareTo(right.GetBoolean());
        }

        if (TryDecimal(left, out var leftNumber) &&
            TryDecimal(right, out var rightNumber))
            return leftNumber.CompareTo(rightNumber);
        return string.Compare(
            ElementToText(left),
            ElementToText(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBoolean(JsonElement element) =>
        element.ValueKind is JsonValueKind.True or JsonValueKind.False;

    private static bool TryDecimal(JsonElement element, out decimal value) =>
        element.ValueKind == JsonValueKind.Number
            ? element.TryGetDecimal(out value)
            : decimal.TryParse(
                ElementToText(element),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value);

    private static bool IsEmpty(JsonElement value) =>
        value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ||
        value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(value.GetString()) ||
        value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0 ||
        value.ValueKind == JsonValueKind.Object && !value.EnumerateObject().Any();

    private static string ElementToText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => element.GetRawText()
    };

    [GeneratedRegex(@"\{\{\s*([^{}]+?)\s*\}\}")]
    private static partial Regex TemplateTokenRegex();
}
