using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Features.Workflows.Command;
using OVCMOVE.Application.Features.Workflows.Query;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/workflows")]
public sealed class WorkflowController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetByRace(
        [FromQuery] Guid raceId,
        [FromQuery] string? cardKey,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            new GetWorkflowsQuery(raceId, cardKey), cancellationToken)));

    [HttpGet("catalog")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            new GetWorkflowCatalogQuery(), cancellationToken)));

    [HttpGet("{workflowId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetDetail(
        Guid workflowId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            new GetWorkflowDetailQuery(workflowId), cancellationToken)));

    [HttpPost("races/{raceId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> Create(
        Guid raceId,
        [FromBody] WorkflowContract.CreateWorkflowRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            request.ToCommand(raceId), cancellationToken)));

    [HttpPut("{workflowId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> Update(
        Guid workflowId,
        [FromBody] WorkflowContract.UpdateWorkflowRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            request.ToCommand(workflowId), cancellationToken)));

    [HttpPut("{workflowId:guid}/status")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> ChangeStatus(
        Guid workflowId,
        [FromBody] WorkflowContract.ChangeWorkflowStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            new ChangeWorkflowStatusCommand
            {
                WorkflowId = workflowId,
                ExpectedModifiedAt = request.ExpectedModifiedAt,
                Status = request.Status
            }, cancellationToken)));

    [HttpDelete("{workflowId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> Delete(
        Guid workflowId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            new DeleteWorkflowCommand { WorkflowId = workflowId },
            cancellationToken)));

    [HttpPost("{workflowId:guid}/execute")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> Execute(
        Guid workflowId,
        [FromBody] WorkflowContract.ExecuteWorkflowRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            request.ToCommand(workflowId), cancellationToken)));

    [HttpGet("{workflowId:guid}/runs")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetRuns(
        Guid workflowId,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        Ok(ApiResponse.Success(await _mediator.Send(
            new GetWorkflowRunsQuery(workflowId, limit), cancellationToken)));
}
