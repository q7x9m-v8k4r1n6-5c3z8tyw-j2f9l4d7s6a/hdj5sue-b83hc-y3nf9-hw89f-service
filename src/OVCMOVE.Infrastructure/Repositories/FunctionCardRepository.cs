using System.Data;
using Dapper;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardInventory;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public sealed class FunctionCardRepository(IDbExecutor db) : IFunctionCardRepository
{
    public async Task<IReadOnlyCollection<FunctionCardReadRow>> GetByRaceAsync(
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        (await db.QueryAsync<FunctionCardReadRow>(
            FunctionCardQueries.SelectByRace,
            new { RaceId = raceId },
            cancellationToken)).ToArray();

    public Task<FunctionCardReadRow?> GetDetailAsync(
        Guid cardId,
        CancellationToken cancellationToken = default) =>
        db.QueryFirstOrDefaultAsync<FunctionCardReadRow>(
            FunctionCardQueries.SelectDetail,
            new { CardId = cardId },
            cancellationToken);

    public Task<FunctionCard?> GetByIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default) =>
        db.QueryFirstOrDefaultAsync<FunctionCard>(
            FunctionCardQueries.SelectEntityById,
            new { CardId = cardId },
            cancellationToken);

    public Task<FunctionCard?> GetByKeyAsync(
        Guid raceId,
        string cardKey,
        CancellationToken cancellationToken = default) =>
        db.QueryFirstOrDefaultAsync<FunctionCard>(
            FunctionCardQueries.SelectEntityByKey,
            new { RaceId = raceId, CardKey = cardKey },
            cancellationToken);

    public async Task CreateAsync(FunctionCard card, CancellationToken cancellationToken = default)
    {
        var affected = await db.ExecuteAsync(FunctionCardQueries.Insert, card, cancellationToken);
        PersistenceWriteGuard.EnsureInserted(affected, nameof(FunctionCard));
    }

    public async Task<bool> UpdateAsync(
        FunctionCard card,
        DateTime expectedModifiedAt,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters(card);
        parameters.Add("ExpectedModifiedAt", expectedModifiedAt, DbType.DateTime2);
        var updated = await db.QueryFirstOrDefaultAsync<int>(
            FunctionCardQueries.Update,
            parameters,
            cancellationToken);
        return updated == 1;
    }

    public async Task<bool> AssignTeamAsync(
        Guid cardId,
        Guid? teamId,
        string actor,
        DateTime expectedModifiedAt,
        DateTime modifiedAt,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("CardId", cardId);
        parameters.Add("TeamId", teamId);
        parameters.Add("Actor", actor);
        parameters.Add("ExpectedModifiedAt", expectedModifiedAt, DbType.DateTime2);
        parameters.Add("ModifiedAt", modifiedAt, DbType.DateTime2);
        return await db.ExecuteAsync(
            FunctionCardQueries.AssignTeam,
            parameters,
            cancellationToken) == 1;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid cardId,
        string actor,
        DateTime modifiedAt,
        CancellationToken cancellationToken = default) =>
        await db.QueryFirstOrDefaultAsync<int>(
            FunctionCardQueries.SoftDelete,
            new { CardId = cardId, Actor = actor, ModifiedAt = modifiedAt },
            cancellationToken) == 1;

    public async Task<IReadOnlyCollection<TeamCardInventoryItemModel>> GetByTeamIdAsync(
        Guid raceId, 
        Guid teamId, 
        string excludedStatus,
        CancellationToken cancellationToken = default) =>
        (await db.QueryAsync<TeamCardInventoryItemModel>(
            FunctionCardQueries.SelectByTeamId,
            new { RaceId = raceId, TeamId = teamId, ExcludedStatus = excludedStatus }, // Map đúng tên biến
            cancellationToken)).ToArray();

    public Task<string?> GetCardDescriptionByIdAsync(
        Guid cardId, 
        Guid teamId, 
        string excludedStatus,
        CancellationToken cancellationToken = default) =>
        db.QueryFirstOrDefaultAsync<string>(
            FunctionCardQueries.SelectCardDescriptionById,
            new { CardId = cardId, TeamId = teamId, ExcludedStatus = excludedStatus }, // Map đúng tên biến
            cancellationToken);
}
