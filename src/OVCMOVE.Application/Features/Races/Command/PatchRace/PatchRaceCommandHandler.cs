using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Races.Command.PatchRace;

public class PatchRaceCommandHandler :
    IRequestHandler<PatchRaceCommand, RaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BoothPatchProcessor _boothProcessor;
    private readonly RaceTeamPatchProcessor _teamProcessor;
    private readonly RaceOrganizerPatchProcessor _organizerProcessor;

    public PatchRaceCommandHandler(
        IRaceRepository raceRepository,
        IBlobStorageService blobStorageService,
        IUnitOfWork unitOfWork,
        BoothPatchProcessor boothProcessor,
        RaceTeamPatchProcessor teamProcessor,
        RaceOrganizerPatchProcessor organizerProcessor)
    {
        _raceRepository = raceRepository;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
        _boothProcessor = boothProcessor;
        _teamProcessor = teamProcessor;
        _organizerProcessor = organizerProcessor;
    }

    /// <summary>Applies one race patch and its related collections atomically.</summary>
    public async Task<RaceDetailResultModel?> Handle(
        PatchRaceCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.ExpectedModifiedAt == default)
        {
            throw new ApplicationValidationException(
                "ExpectedModifiedAt là bắt buộc để cập nhật an toàn.");
        }

        var race = await _raceRepository.GetByIdAsync(
            request.RaceId,
            cancellationToken);
        if (race is null)
        {
            return null;
        }

        if (string.Equals(
            race.Status,
            RaceConstants.RaceStatus.Completed,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationConflictException(
                "Không thể cập nhật trận đấu đã kết thúc.");
        }

        var previousCoverUrl = race.CoverUrl;
        var uploadedCoverUrl = await UploadCoverIfProvidedAsync(
            request,
            cancellationToken);

        var actor = request.GetActorOrSystem();
        var now = DateTime.UtcNow;

        try
        {
            await _unitOfWork.BeginAsync(cancellationToken);
            RacePatchMapper.Apply(race, request, actor, now);
            var updated = await _raceRepository.UpdateAsync(
                race,
                request.ExpectedModifiedAt,
                cancellationToken);
            if (!updated)
            {
                throw new ConcurrencyConflictException(
                    "Trận đấu đã được người khác cập nhật. Vui lòng tải lại dữ liệu trước khi lưu.");
            }

            await _boothProcessor.ApplyAsync(
                request,
                actor,
                now,
                cancellationToken);
            await _teamProcessor.ApplyAsync(
                request,
                actor,
                now,
                cancellationToken);
            await _organizerProcessor.ApplyAsync(
                request,
                actor,
                now,
                cancellationToken);

            // Do not compensate blobs after an ambiguously canceled commit.
            await _unitOfWork.CommitAsync(CancellationToken.None);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            if (uploadedCoverUrl is not null)
            {
                await _blobStorageService.TryDeleteAsync(
                    uploadedCoverUrl,
                    CancellationToken.None);
            }

            throw;
        }

        if (uploadedCoverUrl is not null &&
            !string.IsNullOrWhiteSpace(previousCoverUrl))
        {
            await _blobStorageService.TryDeleteAsync(
                previousCoverUrl,
                CancellationToken.None);
        }

        return await _raceRepository.GetDetailAsync(
            request.RaceId,
            cancellationToken);
    }

    private async Task<string?> UploadCoverIfProvidedAsync(
        PatchRaceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CoverImage is null)
        {
            return null;
        }

        var coverUrl = await _blobStorageService.UploadAsync(
            request.CoverImage.Stream,
            request.CoverImage.FileName,
            request.CoverImage.ContentType,
            cancellationToken);

        request.BasicInfo ??= new PatchRaceCommand.BasicInfoPatchModel();
        request.BasicInfo.CoverUrl = coverUrl;
        return coverUrl;
    }
}
