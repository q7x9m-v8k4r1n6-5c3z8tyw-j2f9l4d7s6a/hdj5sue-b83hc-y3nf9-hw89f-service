using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Races.Command.CreateRace;

public class CreateRaceCommandHandler :
    IRequestHandler<CreateRaceCommand, Guid>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothRepository _boothRepository;
    private readonly IBoothOrganizerRepository _boothOrganizerRepository;
    private readonly IRaceTeamRepository _raceTeamRepository;
    private readonly IRaceOrganizerRepository _raceOrganizerRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateRaceRelationValidator _relationValidator;

    public CreateRaceCommandHandler(
        IRaceRepository raceRepository,
        IBoothRepository boothRepository,
        IBoothOrganizerRepository boothOrganizerRepository,
        IRaceTeamRepository raceTeamRepository,
        IRaceOrganizerRepository raceOrganizerRepository,
        IBlobStorageService blobStorageService,
        IUnitOfWork unitOfWork,
        CreateRaceRelationValidator relationValidator)
    {
        _raceRepository = raceRepository;
        _boothRepository = boothRepository;
        _boothOrganizerRepository = boothOrganizerRepository;
        _raceTeamRepository = raceTeamRepository;
        _raceOrganizerRepository = raceOrganizerRepository;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
        _relationValidator = relationValidator;
    }

    /// <summary>Creates a race and all selected relationships in one database transaction.</summary>
    public async Task<Guid> Handle(CreateRaceCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateRaceFactory.Validate(request);
        await _relationValidator.ValidateAsync(request, cancellationToken);

        var raceId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var actor = request.GetActorOrSystem();
        string? coverUrl = null;

        if (request.CoverImage is not null)
        {
            coverUrl = await _blobStorageService.UploadAsync(
                request.CoverImage.Stream,
                request.CoverImage.FileName,
                request.CoverImage.ContentType,
                cancellationToken: cancellationToken);
        }

        try
        {
            await _unitOfWork.BeginAsync(cancellationToken);
            var race = CreateRaceFactory.CreateRace(
                request,
                raceId,
                coverUrl,
                actor,
                now);
            await _raceRepository.CreateAsync(
                race,
                cancellationToken);

            foreach (var boothInput in request.Booths ?? [])
            {
                var booth = CreateRaceFactory.CreateBooth(
                    boothInput,
                    raceId,
                    actor,
                    now);
                await _boothRepository.CreateAsync(
                    booth,
                    cancellationToken);

                foreach (var organizerId in
                         (boothInput.OrganizerIds ?? []).Distinct())
                {
                    await _boothOrganizerRepository.CreateAsync(
                        CreateRaceFactory.CreateBoothOrganizer(
                            raceId,
                            booth.Id,
                            organizerId,
                            actor,
                            now),
                        cancellationToken);
                }
            }

            foreach (var teamId in (request.TeamIds ?? []).Distinct())
            {
                await _raceTeamRepository.CreateAsync(
                    CreateRaceFactory.CreateRaceTeam(
                        raceId,
                        teamId,
                        actor,
                        now),
                    cancellationToken);
            }

            foreach (var organizerId in
                     (request.OrganizerIds ?? []).Distinct())
            {
                await _raceOrganizerRepository.CreateAsync(
                    CreateRaceFactory.CreateRaceOrganizer(
                        raceId,
                        organizerId,
                        actor,
                        now),
                    cancellationToken);
            }

            // Do not delete the uploaded cover after an ambiguously canceled commit.
            await _unitOfWork.CommitAsync(CancellationToken.None);
            return raceId;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            if (coverUrl is not null)
            {
                await _blobStorageService.TryDeleteAsync(
                    coverUrl,
                    cancellationToken: CancellationToken.None);
            }

            throw;
        }
    }

}
