using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.Workflows.Common;

internal static class WorkflowCommandRules
{
    public static void ValidateIdentity(Guid raceId, Guid cardId, string name)
    {
        if (raceId == Guid.Empty)
            throw new ApplicationValidationException("RaceId là bắt buộc.");
        ValidateIdentity(cardId, name);
    }

    public static void ValidateIdentity(Guid cardId, string name)
    {
        if (cardId == Guid.Empty)
            throw new ApplicationValidationException("CardId là bắt buộc.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255)
            throw new ApplicationValidationException("Tên workflow phải có từ 1 đến 255 ký tự.");
    }

    public static void ValidateWorkflowId(Guid workflowId)
    {
        if (workflowId == Guid.Empty)
            throw new ApplicationValidationException("WorkflowId là bắt buộc.");
    }

    public static void ValidateConcurrency(Guid workflowId, DateTime expectedModifiedAt)
    {
        ValidateWorkflowId(workflowId);
        if (expectedModifiedAt == default)
            throw new ApplicationValidationException("ExpectedModifiedAt là bắt buộc.");
    }

    public static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ApplicationValidationException(
                "Trạng thái workflow là bắt buộc.");

        var normalizedStatus = status.Trim().ToLowerInvariant();
        if (normalizedStatus is not (
            WorkflowConstants.Status.Draft or
            WorkflowConstants.Status.Published or
            WorkflowConstants.Status.Disabled))
            throw new ApplicationValidationException(
                "Trạng thái workflow không hợp lệ.");

        return normalizedStatus;
    }

    public static string TriggerForCard(string category) =>
        string.Equals(
            category,
            FunctionCardConstants.Category.Defense,
            StringComparison.OrdinalIgnoreCase)
            ? WorkflowConstants.Trigger.Attacked
            : WorkflowConstants.Trigger.Activated;
}
