using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Organizer.Command.ChangeOrganizerStatus;

public class ChangeOrganizerStatusCommand : IRequest<OrganizerResultModel.ChangeOrganizerStatusResultModel>
{
    public int OrganizerId { get; init; }

    public bool IsActive { get; init; }
}
