namespace OVCMOVE.Domain.Common;

public abstract class BaseEntity<Tid>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}