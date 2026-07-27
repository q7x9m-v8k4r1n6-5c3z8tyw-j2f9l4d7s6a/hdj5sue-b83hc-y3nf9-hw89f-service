using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Rbac.Permissions.Command.CreatePermission;
using OVCMOVE.Application.Features.Rbac.Permissions.Command.UpdatePermission;
using OVCMOVE.Application.Features.Rbac.Roles.Command.CreateRole;
using OVCMOVE.Application.Features.Rbac.Roles.Command.UpdateRole;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Api.Mapping;

public static class RbacContractMapping
{
    /// <summary>Maps a role contract to a create command.</summary>
    public static CreateRoleCommand ToCreateCommand(
        this RbacContract.UpsertRoleRequest request) => new()
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description
        };

    /// <summary>Maps a role contract to an update command.</summary>
    public static UpdateRoleCommand ToUpdateCommand(
        this RbacContract.UpsertRoleRequest request,
        Guid roleId) => new()
        {
            RoleId = roleId,
            Name = request.Name,
            Code = request.Code,
            Description = request.Description
        };

    /// <summary>Maps a permission contract to a create command.</summary>
    public static CreatePermissionCommand ToCreateCommand(
        this RbacContract.UpsertPermissionRequest request) => new()
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Module = request.Module,
            Action = request.Action
        };

    /// <summary>Maps a permission contract to an update command.</summary>
    public static UpdatePermissionCommand ToUpdateCommand(
        this RbacContract.UpsertPermissionRequest request,
        Guid permissionId) => new()
        {
            PermissionId = permissionId,
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Module = request.Module,
            Action = request.Action
        };

    public static RbacContract.RoleResponse ToResponse(
        this RoleSummaryModel result) => new()
        {
            Id = result.Id,
            Name = result.Name,
            Code = result.Code,
            Description = result.Description,
            IsSystem = result.IsSystem,
            CreatedAt = result.CreatedAt
        };

    public static RbacContract.PermissionResponse ToResponse(
        this PermissionSummaryModel result) => new()
        {
            Id = result.Id,
            Name = result.Name,
            Code = result.Code,
            Description = result.Description,
            Module = result.Module,
            Action = result.Action,
            IsSystem = result.IsSystem
        };

    public static RbacContract.UserRoleAssignmentResponse ToResponse(
        this UserRoleAssignmentModel result) => new()
        {
            UserId = result.UserId,
            RoleId = result.RoleId
        };

    public static RbacContract.RolePermissionAssignmentResponse ToResponse(
        this RolePermissionAssignmentModel result) => new()
        {
            RoleId = result.RoleId,
            PermissionId = result.PermissionId
        };
}
