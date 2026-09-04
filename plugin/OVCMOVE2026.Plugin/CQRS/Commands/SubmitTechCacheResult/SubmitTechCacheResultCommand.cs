using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE2026.Plugin.CQRS.Commands.SubmitTechCacheResult;

public enum TechCacheResultType { Success, Fail }

public sealed record SubmitTechCacheResultCommand(
    Guid MissionId,
    Guid TeamId,
    string Code,
    TechCacheResultType Result,
    FileUploadModel Video
) : IRequest<SubmitTechCacheResultResult>;

public class SubmitTechCacheResultResult
{
    public bool IsSuccess { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsForbidden { get; set; }
    public bool IsInvalidCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ScoreDelta { get; set; }
    public bool IsMapPieceReward { get; set; }
}