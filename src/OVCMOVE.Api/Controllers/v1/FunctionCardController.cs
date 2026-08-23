using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Features.FunctionCards.Command;
using OVCMOVE.Application.Features.FunctionCards.Query.GetFunctionCards;
using OVCMOVE.Application.Features.FunctionCards.Query.GetFunctionCardDetail;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/function-cards")]
public sealed class FunctionCardController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetByRace([FromQuery] Guid raceId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(new GetFunctionCardsQuery(raceId), cancellationToken)));

    [HttpGet("{cardId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetDetail(Guid cardId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(new GetFunctionCardDetailQuery(cardId), cancellationToken)));

    [HttpPost("races/{raceId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> Create(
        Guid raceId,
        [FromBody] FunctionCardContract.MutationRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(request.ToCommand(raceId), cancellationToken)));

    [HttpPut("{cardId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> Update(
        Guid cardId,
        [FromBody] FunctionCardContract.UpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(request.ToCommand(cardId), cancellationToken)));

    [HttpPut("{cardId:guid}/team")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> AssignTeam(
        Guid cardId,
        [FromBody] FunctionCardContract.AssignTeamRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(new AssignFunctionCardTeamCommand
        {
            CardId = cardId,
            TeamId = request.TeamId,
            ExpectedModifiedAt = request.ExpectedModifiedAt
        }, cancellationToken)));

    [HttpDelete("{cardId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> Delete(Guid cardId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            new DeleteFunctionCardCommand { CardId = cardId }, cancellationToken)));
}
