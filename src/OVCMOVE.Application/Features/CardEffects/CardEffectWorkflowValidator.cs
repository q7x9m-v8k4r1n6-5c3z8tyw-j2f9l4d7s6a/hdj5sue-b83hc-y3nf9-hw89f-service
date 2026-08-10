namespace OVCMOVE.Application.Features.CardEffects;

public sealed class CardEffectWorkflowValidator
{
    private const int MaximumSteps = 30;

    public IReadOnlyList<string> Validate(
        CardEffectWorkflowDefinition definition,
        IReadOnlySet<string> conditionTypes,
        IReadOnlySet<string> actionTypes)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var errors = new List<string>();

        if (definition.SchemaVersion !=
            CardEffectWorkflowDefinition.CurrentSchemaVersion)
        {
            errors.Add($"Unsupported schema version: {definition.SchemaVersion}.");
        }

        if (definition.WorkflowVersionId == Guid.Empty)
        {
            errors.Add("WorkflowVersionId is required.");
        }

        if (definition.Version <= 0)
        {
            errors.Add("Version must be greater than zero.");
        }

        if (!IsSafeTypeCode(definition.TriggerType))
        {
            errors.Add("TriggerType must be a safe, non-empty type code.");
        }

        if (definition.Actions.Count == 0)
        {
            errors.Add("At least one action is required.");
        }

        if (definition.Conditions.Count + definition.Actions.Count > MaximumSteps)
        {
            errors.Add($"A workflow cannot contain more than {MaximumSteps} steps.");
        }

        ValidateSteps(
            definition.Conditions,
            conditionTypes,
            "condition",
            errors);
        ValidateSteps(
            definition.Actions,
            actionTypes,
            "action",
            errors);

        if (definition.Policy.MaximumTriggers <= 0)
        {
            errors.Add("MaximumTriggers must be greater than zero.");
        }

        if (!CardEffectConsumeWhen.Supported.Contains(
            definition.Policy.ConsumeWhen))
        {
            errors.Add($"Unsupported ConsumeWhen value: {definition.Policy.ConsumeWhen}.");
        }

        if (definition.Policy.ExpiresAfterSeconds is <= 0)
        {
            errors.Add("ExpiresAfterSeconds must be greater than zero when set.");
        }

        var hasDistinctBy = !string.IsNullOrWhiteSpace(
            definition.Policy.DistinctBy);
        var hasDistinctLimit = definition.Policy.MaximumDistinctValues.HasValue;
        if (hasDistinctBy != hasDistinctLimit)
        {
            errors.Add(
                "DistinctBy and MaximumDistinctValues must be configured together.");
        }
        else if (definition.Policy.MaximumDistinctValues is <= 0)
        {
            errors.Add("MaximumDistinctValues must be greater than zero when set.");
        }

        return errors;
    }

    private static void ValidateSteps(
        IReadOnlyList<CardEffectStepDefinition> steps,
        IReadOnlySet<string> supportedTypes,
        string stepKind,
        ICollection<string> errors)
    {
        foreach (var step in steps)
        {
            if (!IsSafeTypeCode(step.Type))
            {
                errors.Add($"Every {stepKind} must have a safe type code.");
                continue;
            }

            if (!supportedTypes.Contains(step.Type))
            {
                errors.Add($"Unsupported {stepKind} type: {step.Type}.");
            }
        }
    }

    private static bool IsSafeTypeCode(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.All(character =>
            char.IsLower(character) ||
            char.IsDigit(character) ||
            character is '.' or '_' or '-');
}

