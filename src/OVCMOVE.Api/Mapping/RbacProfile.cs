using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Rbac.Permissions.Command.CreatePermission;
using OVCMOVE.Application.Features.Rbac.Permissions.Command.UpdatePermission;
using OVCMOVE.Application.Features.Rbac.Roles.Command.CreateRole;
using OVCMOVE.Application.Features.Rbac.Roles.Command.UpdateRole;

namespace OVCMOVE.Api.Mapping;

public class RbacProfile : Profile
{
    public RbacProfile()
    {
        CreateMap<RbacContract.UpsertRoleRequest, CreateRoleCommand>();
        CreateMap<RbacContract.UpsertRoleRequest, UpdateRoleCommand>();

        CreateMap<RbacContract.UpsertPermissionRequest, CreatePermissionCommand>();
        CreateMap<RbacContract.UpsertPermissionRequest, UpdatePermissionCommand>();
    }
}
