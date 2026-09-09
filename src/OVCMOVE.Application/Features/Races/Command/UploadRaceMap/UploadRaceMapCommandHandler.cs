using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Races.Command.UploadRaceMap;

public class UploadRaceMapCommandHandler : IRequestHandler<UploadRaceMapCommand, string>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBlobStorageService _blobStorageService;

    public UploadRaceMapCommandHandler(
        IRaceRepository raceRepository,
        IBlobStorageService blobStorageService)
    {
        _raceRepository = raceRepository;
        _blobStorageService = blobStorageService;
    }

    /// <summary>Uploads race map image to dedicated blob container and updates race record.</summary>
    public async Task<string> Handle(
        UploadRaceMapCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.RaceId == Guid.Empty)
        {
            throw new ApplicationValidationException("RaceId không được để trống.");
        }

        if (request.File is null || request.File.Stream is null || string.IsNullOrWhiteSpace(request.File.FileName))
        {
            throw new ApplicationValidationException("File tải lên không hợp lệ.");
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

        var previousMapUrl = race.MapImageUrl;
        var uploadedMapUrl = await _blobStorageService.UploadAsync(
            request.File.Stream,
            request.File.FileName,
            request.File.ContentType,
            _blobStorageService.MapContainerName,
            cancellationToken);

        var actor = request.GetActorOrSystem();
        var now = DateTime.UtcNow;

        try
        {
            var updated = await _raceRepository.UpdateMapImageUrlAsync(
                request.RaceId,
                uploadedMapUrl,
                actor,
                now,
                cancellationToken);

            if (!updated)
            {
                throw new ApplicationConflictException(
                    "Không thể cập nhật sơ đồ trận đấu vào cơ sở dữ liệu.");
            }
        }
        catch
        {
            await _blobStorageService.TryDeleteAsync(
                uploadedMapUrl,
                _blobStorageService.MapContainerName,
                CancellationToken.None);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousMapUrl) &&
            !string.Equals(previousMapUrl, uploadedMapUrl, StringComparison.OrdinalIgnoreCase))
        {
            await _blobStorageService.TryDeleteAsync(
                previousMapUrl,
                _blobStorageService.MapContainerName,
                CancellationToken.None);
        }

        return uploadedMapUrl;
    }
}
