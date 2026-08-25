using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardInventory;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IFunctionCardRepository
{
    Task<IReadOnlyCollection<FunctionCardReadRow>> GetByRaceAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<FunctionCardReadRow?> GetDetailAsync(Guid cardId, CancellationToken cancellationToken = default);
    Task<FunctionCard?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken = default);
    Task<FunctionCard?> GetByKeyAsync(Guid raceId, string cardKey, CancellationToken cancellationToken = default);
    Task CreateAsync(FunctionCard card, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(FunctionCard card, DateTime expectedModifiedAt, CancellationToken cancellationToken = default);
    Task<bool> AssignTeamAsync(Guid cardId, Guid? teamId, string actor, DateTime expectedModifiedAt, DateTime modifiedAt, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid cardId, string actor, DateTime modifiedAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamCardInventoryItemModel>> GetByTeamIdAsync(Guid raceId, Guid teamId, string excludedStatus, CancellationToken cancellationToken = default);
    Task<string?> GetCardDescriptionByIdAsync(Guid cardId, Guid teamId, string excludedStatus, CancellationToken cancellationToken = default);
}
