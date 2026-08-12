using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Commands.RequestEntryToBooth;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Test.Application;

public sealed class HiddenBoothCreationTests
{
    [Fact]
    public void CreateRace_MapsAndCreatesHiddenBoothWithExistingFactory()
    {
        var organizerId = Guid.NewGuid();
        var request = new RaceContract.CreateNewRaceRequest
        {
            Booths =
            [
                new RaceContract.CreateNewRaceRequest.BoothInfoModel
                {
                    Name = "  Trạm bí mật  ",
                    Place = "  Khu A  ",
                    Description = "  Thử thách ẩn  ",
                    IsHidden = true,
                    OrganizerIds = [organizerId]
                }
            ]
        };

        var command = request.ToCommand();
        var input = Assert.Single(command.Booths);
        var booth = CreateRaceFactory.CreateBooth(
            input,
            Guid.NewGuid(),
            "tester",
            DateTime.UtcNow);

        Assert.True(input.IsHidden);
        Assert.Equal([organizerId], input.OrganizerIds);
        Assert.True(booth.IsHidden);
        Assert.Equal("Trạm bí mật", booth.Name);
        Assert.Equal("Khu A", booth.Place);
        Assert.Equal("Thử thách ẩn", booth.Description);
    }

    [Fact]
    public void CreateRace_RegularBoothRemainsTheBackwardCompatibleDefault()
    {
        var request = new RaceContract.CreateNewRaceRequest
        {
            Booths =
            [
                new RaceContract.CreateNewRaceRequest.BoothInfoModel
                {
                    Name = "Trạm thường",
                    Place = "Khu B"
                }
            ]
        };

        var input = Assert.Single(request.ToCommand().Booths);
        var booth = CreateRaceFactory.CreateBooth(
            input,
            Guid.NewGuid(),
            "tester",
            DateTime.UtcNow);

        Assert.False(input.IsHidden);
        Assert.False(booth.IsHidden);
    }

    [Fact]
    public void PatchRace_MapsHiddenBoothWithoutASeparateCreateFlow()
    {
        var request = new RaceContract.PatchRaceRequest
        {
            Booths = new RaceContract.PatchRaceRequest.BoothPatchModel
            {
                Add =
                [
                    new RaceContract.PatchRaceRequest.CreateBoothPatchItem
                    {
                        Name = "Trạm ẩn",
                        Place = "Vườn",
                        Description = "Tìm mật mã",
                        IsHidden = true
                    }
                ]
            }
        };

        var command = request.ToCommand(Guid.NewGuid());
        var booth = Assert.Single(command.Booths!.Add!);

        Assert.True(booth.IsHidden);
        Assert.Equal("Trạm ẩn", booth.Name);
        Assert.Equal("Vườn", booth.Place);
        Assert.Equal("Tìm mật mã", booth.Description);
    }

    [Fact]
    public async Task BoothPatchProcessor_CreatesHiddenBoothAndOrganizerRelation()
    {
        var raceId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var booths = new BoothRepositoryStub();
        var assignments = new BoothOrganizerRepositoryStub();
        var processor = new BoothPatchProcessor(
            booths,
            assignments,
            new OrganizerRepositoryStub([organizerId]));
        var command = HiddenBoothPatch(raceId, organizerId);

        await processor.ApplyAsync(
            command,
            "tester",
            DateTime.UtcNow,
            CancellationToken.None);

        var booth = Assert.Single(booths.Created);
        Assert.True(booth.IsHidden);
        Assert.Equal(raceId, booth.RaceId);
        var assignment = Assert.Single(assignments.Created);
        Assert.Equal(booth.Id, assignment.BoothId);
        Assert.Equal(organizerId, assignment.OrganizerId);
        Assert.Equal(raceId, assignment.RaceId);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task CreatedHiddenBooth_UsesTwoRegularBoothsForEntryQuota(
        int completedRegularBooths,
        bool expectedSuccess)
    {
        var team = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Team A"
        };
        var booth = CreateRaceFactory.CreateBooth(
            new CreateRaceCommand.CreateBoothModel
            {
                Name = "Trạm ẩn",
                Place = "Khu bí mật",
                IsHidden = true
            },
            Guid.NewGuid(),
            "tester",
            DateTime.UtcNow);
        var repository = new InMemoryBoothRepository(booth);
        var handler = new RequestEntryToBoothCommandHandler(
            repository,
            new ValidBoothRaceRepository(new BoothProgressResultModel
            {
                IsTeamInRace = true,
                CompletedRegularBooths = completedRegularBooths,
                CompletedHiddenBooths = 0
            }),
            new BoothNotificationSpy(),
            new StubTeamUserRepository(team));

        var result = await handler.Handle(
            new RequestEntryToBoothCommand
            {
                BoothId = booth.Id,
                TeamId = team.Id
            },
            CancellationToken.None);

        Assert.Equal(expectedSuccess, result.IsSuccess);
        Assert.Equal(
            expectedSuccess
                ? BoothConstants.BoothStatus.Pending
                : BoothConstants.BoothStatus.Free,
            booth.Status);
        if (!expectedSuccess)
        {
            Assert.Equal(
                BoothParticipationPolicy.ChooseAnotherBoothMessage,
                result.Message);
        }
    }

    [Fact]
    public async Task BoothPatchProcessor_RejectsUnknownOrganizerBeforeWriting()
    {
        var booths = new BoothRepositoryStub();
        var assignments = new BoothOrganizerRepositoryStub();
        var processor = new BoothPatchProcessor(
            booths,
            assignments,
            new OrganizerRepositoryStub([]));

        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            processor.ApplyAsync(
                HiddenBoothPatch(Guid.NewGuid(), Guid.NewGuid()),
                "tester",
                DateTime.UtcNow,
                CancellationToken.None));

        Assert.Empty(booths.Created);
        Assert.Empty(assignments.Created);
    }

    [Fact]
    public async Task BoothPatchProcessor_PropagatesCancellationBeforeWriting()
    {
        var booths = new BoothRepositoryStub();
        var assignments = new BoothOrganizerRepositoryStub();
        var processor = new BoothPatchProcessor(
            booths,
            assignments,
            new OrganizerRepositoryStub([]));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            processor.ApplyAsync(
                HiddenBoothPatch(Guid.NewGuid()),
                "tester",
                DateTime.UtcNow,
                cancellation.Token));

        Assert.Empty(booths.Created);
        Assert.Empty(assignments.Created);
    }

    [Fact]
    public void RaceDetail_ExposesHiddenBoothForFrontendGrouping()
    {
        var result = new RaceDetailResultModel
        {
            Booth =
            [
                new RaceBoothModel
                {
                    Id = Guid.NewGuid(),
                    Name = "Trạm ẩn",
                    Place = "Khu C",
                    IsHidden = true
                }
            ]
        };

        var response = result.ToResponse();

        Assert.True(Assert.Single(response.Booth).IsHidden);
    }

    [Fact]
    public void BoothQueries_PersistAndReloadHiddenFlag()
    {
        Assert.Contains("[IsHidden]", RaceQueries.CreateBoothQuery());
        Assert.Contains("@IsHidden", RaceQueries.CreateBoothQuery());
        Assert.Contains("[IsHidden]", RaceQueries.GetRaceBoothsQuery());
        Assert.Contains("[IsHidden]", RaceQueries.GetBoothsByRaceIdQuery());
    }

    private static PatchRaceCommand HiddenBoothPatch(
        Guid raceId,
        params Guid[] organizerIds) =>
        new()
        {
            RaceId = raceId,
            Booths = new PatchRaceCommand.BoothPatchModel
            {
                Add =
                [
                    new PatchRaceCommand.CreateBoothPatchItem
                    {
                        Name = "Trạm ẩn",
                        Place = "Khu bí mật",
                        Description = "Giải mật mã",
                        IsHidden = true,
                        OrganizerIds = [.. organizerIds]
                    }
                ]
            }
        };

    private sealed class BoothRepositoryStub : IBoothRepository
    {
        public List<Booth> Created { get; } = [];

        public Task<Guid> CreateAsync(
            Booth booth,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Created.Add(booth);
            return Task.FromResult(booth.Id);
        }

        public Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(
            Guid raceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<Booth>>([]);
        }

        public Task<Booth?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Booth?> GetActiveByTeamAndRaceAsync(
            Guid teamId,
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            Booth booth,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryRequestEntryAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryOccupyAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryRejectEntryAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryReleaseAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            Guid boothId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SubmitScoreAndReleaseAsync(
            SubmitBoothScoreModel model,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class BoothOrganizerRepositoryStub :
        IBoothOrganizerRepository
    {
        public List<BoothOrganizer> Created { get; } = [];

        public Task CreateAsync(
            BoothOrganizer boothOrganizer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Created.Add(boothOrganizer);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<BoothOrganizer>> GetByRaceIdAsync(
            Guid raceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<BoothOrganizer>>([]);
        }

        public Task DeleteByBoothIdAsync(
            Guid boothId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BoothOrganizer?> GetByOrganizerAndRaceAsync(
            Guid organizerId,
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> IsAssignedAsync(
            Guid organizerId,
            Guid boothId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class OrganizerRepositoryStub(
        IReadOnlyCollection<Guid> existingIds) : IOrganizerRepository
    {
        public Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
            IEnumerable<Guid> organizerIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<Guid>>(
                organizerIds.Intersect(existingIds).ToArray());
        }

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<User?> GetByIdAsync(
            Guid organizerId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(
            IReadOnlyCollection<GetAllOrganizersResultModel> Items,
            int TotalItems)> GetPageAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<User>> SearchAsync(
            string keyword,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ChangeStatusAsync(
            Guid organizerId,
            string status,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            User organizer,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
