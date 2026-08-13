using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

internal sealed class InMemoryBoothRepository(Booth booth)
    : IBoothRepository
{
    private readonly object _gate = new();

    public Booth Booth { get; } = booth;
    public int CancelledSessionCount { get; private set; }
    public int SubmittedScoreCount { get; private set; }
    public int? LastSubmittedScore { get; private set; }

    public Task<Booth?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Booth?>(Booth.Id == id ? Booth : null);

    public Task<Booth?> GetActiveByTeamAndRaceAsync(
        Guid teamId,
        Guid raceId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var isActive = Booth.TeamId == teamId &&
                Booth.RaceId == raceId &&
                Booth.Status is BoothConstants.BoothStatus.Pending or
                    BoothConstants.BoothStatus.Occupied;
            return Task.FromResult<Booth?>(isActive ? Booth : null);
        }
    }

    public Task<bool> TryRequestEntryAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (Booth.Id != boothId ||
                Booth.Status != BoothConstants.BoothStatus.Free ||
                Booth.TeamId is not null)
            {
                return Task.FromResult(false);
            }

            Booth.Status = BoothConstants.BoothStatus.Pending;
            Booth.TeamId = teamId;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryOccupyAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (Booth.Id != boothId ||
                Booth.Status != BoothConstants.BoothStatus.Pending ||
                Booth.TeamId != teamId)
            {
                return Task.FromResult(false);
            }

            Booth.Status = BoothConstants.BoothStatus.Occupied;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryRejectEntryAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (Booth.Id != boothId ||
                Booth.Status != BoothConstants.BoothStatus.Pending ||
                Booth.TeamId != teamId)
            {
                return Task.FromResult(false);
            }

            Booth.Status = BoothConstants.BoothStatus.Free;
            Booth.TeamId = null;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReleaseAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (Booth.Id != boothId ||
                Booth.Status != BoothConstants.BoothStatus.Occupied ||
                Booth.TeamId != teamId)
            {
                return Task.FromResult(false);
            }

            Booth.Status = BoothConstants.BoothStatus.Free;
            Booth.TeamId = null;
            CancelledSessionCount++;
            return Task.FromResult(true);
        }
    }

    public Task<bool> SubmitScoreAndReleaseAsync(
        SubmitBoothScoreModel model,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (Booth.Id != model.BoothId ||
                Booth.Status != BoothConstants.BoothStatus.Occupied ||
                Booth.TeamId != model.TeamId)
            {
                throw new ApplicationConflictException(
                    "Đội không còn chiếm trạm này.");
            }

            Booth.Status = BoothConstants.BoothStatus.Free;
            Booth.TeamId = null;
            SubmittedScoreCount++;
            LastSubmittedScore = model.Score;
            return Task.FromResult(true);
        }
    }

    public Task<Guid> CreateAsync(Booth value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> UpdateAsync(Booth value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class AssignedBoothOrganizerRepository(bool isAssigned = true)
    : IBoothOrganizerRepository
{
    public Task<bool> IsAssignedAsync(
        Guid organizerId,
        Guid boothId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(isAssigned);

    public Task CreateAsync(BoothOrganizer value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeleteByBoothIdAsync(Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<BoothOrganizer?> GetByOrganizerAndRaceAsync(Guid organizerId, Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<BoothOrganizer>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class ValidBoothRaceRepository(
    BoothProgressResultModel? progress = null) : IRaceRepository
{
    public Task<BoothProgressResultModel> GetBoothProgressAsync(
        Guid raceId,
        Guid teamId,
        Guid boothId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(progress ?? new BoothProgressResultModel
        {
            IsTeamInRace = true
        });

    public Task CreateAsync(Race race, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)> GetPageAsync(int page, int pageSize, Guid? teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> UpdateAsync(Race race, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<List<BoothListResultModel>> GetBoothListAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<(IReadOnlyCollection<ScoringLogResultModel> Items, int TotalItems)> GetScoringLogPageByRaceIdAsync(Guid raceId, Guid? teamId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<(int CompletedRegularBooths, int CompletedHiddenBooths)> GetCompletedBoothStatsAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<int?> GetRaceTeamScoreAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> UpdateRaceTeamScoreAsync(Guid raceId, Guid teamId, int totalScore, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task CreateScoringLogAsync(ScoringLog log, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task CreateRaceMessageAsync(RaceMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(Guid raceId, int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> IsTeamInRaceAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<string?> GetRulesAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class StubTeamUserRepository(User user) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(id == user.Id ? user : null);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<User?> GetByUsernameAnyStatusAsync(string username, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<User?> GetByEmailAnyStatusAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<User?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task AddAsync(User value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdateDisplayNameAsync(Guid id, string displayName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdateGoogleProfileAsync(Guid id, string? displayName, string? avatarUrl, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> SoftDeleteAsync(Guid id, string userType, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class BoothNotificationSpy : IBoothNotificationService
{
    private int _rejectedCount;
    private int _cancelledCount;

    public int RejectedCount => _rejectedCount;
    public int CancelledCount => _cancelledCount;
    public List<(Guid RaceId, Guid TeamId, int Delta)> ScoreChanges { get; } = [];
    public List<(Guid RaceId, Guid BoothId, string Status)> StatusChanges { get; } = [];

    public Task NotifyBoothStatusChangedAsync(Guid raceId, Guid boothId, string status, Guid? teamId, string? teamName, CancellationToken cancellationToken = default)
    {
        lock (StatusChanges)
        {
            StatusChanges.Add((raceId, boothId, status));
        }
        return Task.CompletedTask;
    }

    public Task NotifyRaceScoreChangedAsync(Guid raceId, Guid teamId, int delta, CancellationToken cancellationToken = default)
    {
        lock (ScoreChanges)
        {
            ScoreChanges.Add((raceId, teamId, delta));
        }
        return Task.CompletedTask;
    }

    public Task NotifyBoothEntryRejectedAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _rejectedCount);
        return Task.CompletedTask;
    }

    public Task NotifyBoothEntryCancelledAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _cancelledCount);
        return Task.CompletedTask;
    }

    public Task NotifyRaceMessageAsync(Guid raceId, RaceMessageResultModel message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class UnitOfWorkSpy : IUnitOfWork
{
    private int _beginCount;
    private int _commitCount;
    private int _rollbackCount;

    public int BeginCount => _beginCount;
    public int CommitCount => _commitCount;
    public int RollbackCount => _rollbackCount;

    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _beginCount);
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _commitCount);
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _rollbackCount);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingBoothRepository : IBoothRepository
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

internal sealed class RecordingBoothOrganizerRepository :
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

internal sealed class ExistingOrganizerRepository(
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
