using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Command.AssignFunctionCardTeam;

public sealed class AssignFunctionCardTeamCommandHandler(
    IFunctionCardRepository repository,
    IRaceRepository raceRepository)
    : IRequestHandler<AssignFunctionCardTeamCommand, FunctionCardResultModel>
{
    public async Task<FunctionCardResultModel> Handle(
        AssignFunctionCardTeamCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CardId == Guid.Empty || request.ExpectedModifiedAt == default)
            throw new ApplicationValidationException("CardId và expectedModifiedAt là bắt buộc.");
        var card = await repository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        if (request.TeamId.HasValue &&
            !await raceRepository.IsTeamInRaceAsync(card.RaceId, request.TeamId.Value, cancellationToken))
            throw new ApplicationValidationException("Team được chọn không thuộc race của thẻ.");

        if (!await repository.AssignTeamAsync(
            card.Id, request.TeamId, request.GetActorOrSystem(), request.ExpectedModifiedAt,
            DateTime.UtcNow, cancellationToken))
            throw new ConcurrencyConflictException("Thẻ đã được người khác cập nhật. Vui lòng tải lại.");
        return (await repository.GetDetailAsync(card.Id, cancellationToken))!.ToResult();
    }
}