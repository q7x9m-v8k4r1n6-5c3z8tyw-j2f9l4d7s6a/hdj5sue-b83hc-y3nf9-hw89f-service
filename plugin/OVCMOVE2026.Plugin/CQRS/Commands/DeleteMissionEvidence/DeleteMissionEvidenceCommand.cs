using MediatR;

namespace OVCMOVE2026.Plugin.CQRS.Commands.DeleteMissionEvidence;

public sealed record DeleteMissionEvidenceCommand(
    Guid MissionId, 
    Guid FileId
) : IRequest<bool>;