namespace OVCMOVE.Domain.Common;

public abstract class BaseEntity<TId>
    where TId : notnull
{
    public required TId Id { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
}
