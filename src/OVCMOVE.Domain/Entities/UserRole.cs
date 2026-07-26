using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
