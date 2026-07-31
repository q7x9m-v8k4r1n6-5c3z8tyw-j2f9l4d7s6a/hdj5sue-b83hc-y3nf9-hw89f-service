using OVCMOVE.Application.Common;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;
using OVCMOVE.Application.Features.Rbac;
using OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public class UseCaseValidationTests
{
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(2, 500, 2, Pagination.MaxPageSize)]
    public void Pagination_NormalizesUnsafeClientValues(
        int page,
        int pageSize,
        int expectedPage,
        int expectedPageSize)
    {
        var result = Pagination.Normalize(page, pageSize);

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
    }

    [Fact]
    public void AuditedRequest_UsesSystemOutsideTheHttpPipeline()
    {
        var command = new CreateRaceCommand();

        Assert.Equal("system", command.GetActorOrSystem());
    }

    [Fact]
    public void CreateRace_RejectsBoothWithoutName()
    {
        var command = ValidCreateRace();
        command.Booths.Add(new CreateRaceCommand.CreateBoothModel
        {
            Name = " ",
            Place = "Gate A"
        });

        Assert.Throws<ApplicationValidationException>(
            () => CreateRaceFactory.Validate(command));
    }

    [Fact]
    public void CreateRace_RejectsTextThatExceedsDatabaseBounds()
    {
        var command = ValidCreateRace();
        command.RaceName = new string('x', 256);

        Assert.Throws<ApplicationValidationException>(
            () => CreateRaceFactory.Validate(command));
    }

    [Fact]
    public void PatchRace_RejectsUnknownStatusInsteadOfFallingBackToDraft()
    {
        var race = ValidRace();
        var command = new PatchRaceCommand
        {
            BasicInfo = new PatchRaceCommand.BasicInfoPatchModel
            {
                Status = "not-a-status"
            }
        };

        Assert.Throws<ApplicationValidationException>(() =>
            RacePatchMapper.Apply(
                race,
                command,
                "tester",
                DateTime.UtcNow));
    }

    [Fact]
    public void PatchRace_ValidatesTheResultingTimeRange()
    {
        var race = ValidRace();
        var command = new PatchRaceCommand
        {
            BasicInfo = new PatchRaceCommand.BasicInfoPatchModel
            {
                TimeEnd = race.TimeStart
            }
        };

        Assert.Throws<ApplicationValidationException>(() =>
            RacePatchMapper.Apply(
                race,
                command,
                "tester",
                DateTime.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RbacInput_RejectsMissingRequiredFields(string? value)
    {
        Assert.Throws<ApplicationValidationException>(
            () => RbacInput.Required(value, "Role code"));
    }

    [Fact]
    public void RbacInput_RejectsTextThatExceedsDatabaseBounds()
    {
        Assert.Throws<ApplicationValidationException>(
            () => RbacInput.Required("too-long", "Code", 3));
    }

    [Fact]
    public async Task CreateRace_RejectsMissingRelationshipIds()
    {
        var missingTeamId = Guid.NewGuid();
        var command = ValidCreateRace();
        command.TeamIds.Add(missingTeamId);
        var validator = new CreateRaceRelationValidator(
            new TeamRepositoryStub([]),
            new OrganizerRepositoryStub([]));

        var exception = await Assert.ThrowsAsync<
            ApplicationValidationException>(() =>
            validator.ValidateAsync(command, CancellationToken.None));

        Assert.Contains(missingTeamId.ToString(), exception.Message);
    }

    private static CreateRaceCommand ValidCreateRace() => new()
    {
        RaceName = "MOVE 2026",
        Place = "Ho Chi Minh City",
        TimeStart = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc),
        TimeEnd = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc)
    };

    private static Race ValidRace() => new()
    {
        Id = Guid.NewGuid(),
        RaceName = "MOVE 2026",
        Place = "Ho Chi Minh City",
        Status = "draft",
        TimeStart = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc),
        TimeEnd = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc)
    };

    private sealed class TeamRepositoryStub(
        IReadOnlyCollection<Guid> existingIds) : ITeamRepository
    {
        public Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
            IEnumerable<Guid> teamIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Guid>>(
                teamIds.Intersect(existingIds).ToArray());

        public Task<(
            IReadOnlyCollection<User> Items,
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

        public Task<User?> GetByIdAsync(
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            User team,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class OrganizerRepositoryStub(
        IReadOnlyCollection<Guid> existingIds) : IOrganizerRepository
    {
        public Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
            IEnumerable<Guid> organizerIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Guid>>(
                organizerIds.Intersect(existingIds).ToArray());

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
