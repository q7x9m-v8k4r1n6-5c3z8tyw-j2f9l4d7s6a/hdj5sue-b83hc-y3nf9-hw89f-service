using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Command.CreatePermission;

public class CreatePermissionCommand : AuditedRequest, IRequest<PermissionSummaryModel>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
