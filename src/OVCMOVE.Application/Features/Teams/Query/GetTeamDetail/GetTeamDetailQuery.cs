using MediatR;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Teams.Query.GetTeamDetail;

public sealed record GetTeamDetailQuery(Guid TeamId) : IRequest<User?>;
