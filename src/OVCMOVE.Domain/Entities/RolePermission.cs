using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
