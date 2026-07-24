using MediatR;
using OVCMOVE.Application.DTOs.Team;

namespace OVCMOVE.Application.Features.Teams.Command.ResetTeamPassword;

public sealed record ResetTeamPasswordCommand(Guid TeamId) : IRequest<TeamResponse?>;
