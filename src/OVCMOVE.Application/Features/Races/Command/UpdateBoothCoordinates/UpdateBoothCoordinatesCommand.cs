using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Races.Command.UpdateBoothCoordinates;

public sealed class UpdateBoothCoordinatesCommand : AuditedRequest, IRequest<bool>
{
    public Guid RaceId { get; set; }
    public IReadOnlyCollection<BoothCoordinateItemModel> Coordinates { get; set; } = [];
}
