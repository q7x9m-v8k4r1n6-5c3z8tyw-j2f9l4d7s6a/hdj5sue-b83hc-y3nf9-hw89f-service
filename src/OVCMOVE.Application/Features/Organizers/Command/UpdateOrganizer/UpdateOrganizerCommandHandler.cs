using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Organizers.Command.UpdateOrganizer;
public sealed class UpdateOrganizerCommandHandler(IOrganizerRepository organizers, IRoleRepository roles, IUserRoleRepository userRoles)
    : IRequestHandler<UpdateOrganizerCommand, bool>
{
    public async Task<bool> Handle(UpdateOrganizerCommand request, CancellationToken cancellationToken)
    {
        var organizer = await organizers.GetByIdAsync(request.OrganizerId, cancellationToken);
        if (organizer is null) return false;
        var roleIds = request.RoleIds.Distinct().ToArray();
        if (string.IsNullOrWhiteSpace(request.DisplayName) || roleIds.Length == 0)
            throw new ApplicationValidationException("Tên hiển thị và ít nhất một vai trò là bắt buộc.");
        foreach (var roleId in roleIds)
            if (await roles.GetByIdAsync(roleId, cancellationToken) is null)
                throw new ApplicationNotFoundException("Vai trò được chọn không tồn tại.");
        organizer.DisplayName = request.DisplayName.Trim();
        organizer.Status = request.Status;
        organizer.ModifiedBy = request.GetActorOrSystem(); organizer.ModifiedAt = DateTime.UtcNow;
        if (!await organizers.UpdateAsync(organizer, cancellationToken)) return false;
        var currentIds = await userRoles.GetRoleIdsByUserIdAsync(organizer.Id, cancellationToken);
        foreach (var roleId in currentIds.Except(roleIds)) await userRoles.SoftDeleteAsync(organizer.Id, roleId, organizer.ModifiedBy, organizer.ModifiedAt, cancellationToken);
        foreach (var roleId in roleIds.Except(currentIds)) await userRoles.CreateAsync(new UserRole
        {
            Id = Guid.NewGuid(), UserId = organizer.Id, RoleId = roleId,
            CreatedBy = organizer.ModifiedBy, CreatedAt = organizer.ModifiedAt,
            ModifiedBy = organizer.ModifiedBy, ModifiedAt = organizer.ModifiedAt,
            IsDeleted = false,
        }, cancellationToken);
        return true;
    }
}
