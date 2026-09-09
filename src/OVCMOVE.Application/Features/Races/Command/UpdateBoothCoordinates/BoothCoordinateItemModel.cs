namespace OVCMOVE.Application.Features.Races.Command.UpdateBoothCoordinates;

public sealed class BoothCoordinateItemModel
{
    public Guid BoothId { get; init; }
    public double MapX { get; init; }
    public double MapY { get; init; }
}
