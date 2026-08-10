using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain entity: message sent from race administration to race participants.
/// </summary>
public class RaceMessage : BaseEntity
{
    public Guid RaceId { get; set; }
    public Guid? SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string RecipientKeysJson { get; set; } = "[]";
    public string RecipientLabelsJson { get; set; } = "[]";
    public string Body { get; set; } = string.Empty;
}
