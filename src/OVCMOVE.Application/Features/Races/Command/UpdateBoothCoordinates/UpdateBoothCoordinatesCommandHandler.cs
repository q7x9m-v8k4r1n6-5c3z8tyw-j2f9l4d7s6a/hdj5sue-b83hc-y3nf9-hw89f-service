using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Races.Command.UpdateBoothCoordinates;

public class UpdateBoothCoordinatesCommandHandler : IRequestHandler<UpdateBoothCoordinatesCommand, bool>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothRepository _boothRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBoothCoordinatesCommandHandler(
        IRaceRepository raceRepository,
        IBoothRepository boothRepository,
        IUnitOfWork unitOfWork)
    {
        _raceRepository = raceRepository;
        _boothRepository = boothRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Updates coordinates (MapX, MapY) in batch for booths of a race within a transaction.</summary>
    public async Task<bool> Handle(
        UpdateBoothCoordinatesCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.RaceId == Guid.Empty)
        {
            throw new ApplicationValidationException("RaceId không được để trống.");
        }

        var race = await _raceRepository.GetByIdAsync(
            request.RaceId,
            cancellationToken);
        if (race is null)
        {
            throw new ApplicationNotFoundException(
                $"Không tìm thấy trận đấu với ID: {request.RaceId}");
        }

        if (string.Equals(
            race.Status,
            RaceConstants.RaceStatus.Completed,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationConflictException(
                "Không thể cập nhật sơ đồ trận đấu đã kết thúc.");
        }

        if (request.Coordinates is null)
        {
            throw new ApplicationValidationException("Danh sách tọa độ không được để trống.");
        }

        // Validate range and validity of each coordinate item
        foreach (var item in request.Coordinates)
        {
            if (item.BoothId == Guid.Empty)
            {
                throw new ApplicationValidationException("BoothId không được để trống.");
            }

            if (double.IsNaN(item.MapX) || double.IsInfinity(item.MapX) || item.MapX < 0.0 || item.MapX > 100.0)
            {
                throw new ApplicationValidationException(
                    $"Tọa độ MapX của trạm '{item.BoothId}' phải nằm trong khoảng từ 0.0 đến 100.0.");
            }

            if (double.IsNaN(item.MapY) || double.IsInfinity(item.MapY) || item.MapY < 0.0 || item.MapY > 100.0)
            {
                throw new ApplicationValidationException(
                    $"Tọa độ MapY của trạm '{item.BoothId}' phải nằm trong khoảng từ 0.0 đến 100.0.");
            }
        }

        // Validate no duplicate booth IDs in payload
        var duplicateBoothIds = request.Coordinates
            .GroupBy(c => c.BoothId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicateBoothIds.Length > 0)
        {
            throw new ApplicationValidationException(
                $"Danh sách tọa độ chứa trạm bị trùng lặp: {string.Join(", ", duplicateBoothIds)}.");
        }

        // Validate that all booths belong to this RaceId
        var existingBooths = await _boothRepository.GetByRaceIdAsync(
            request.RaceId,
            cancellationToken);
        var validBoothIds = existingBooths.Select(b => b.Id).ToHashSet();

        var invalidBoothIds = request.Coordinates
            .Where(c => !validBoothIds.Contains(c.BoothId))
            .Select(c => c.BoothId)
            .ToArray();

        if (invalidBoothIds.Length > 0)
        {
            throw new ApplicationValidationException(
                $"Các trạm sau không thuộc về giải đấu '{request.RaceId}': {string.Join(", ", invalidBoothIds)}.");
        }

        if (request.Coordinates.Count == 0)
        {
            return true;
        }

        var actor = request.GetActorOrSystem();
        var now = DateTime.UtcNow;

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await _boothRepository.UpdateCoordinatesBatchAsync(
                request.RaceId,
                request.Coordinates,
                now,
                actor,
                cancellationToken);

            await _unitOfWork.CommitAsync(CancellationToken.None);
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
