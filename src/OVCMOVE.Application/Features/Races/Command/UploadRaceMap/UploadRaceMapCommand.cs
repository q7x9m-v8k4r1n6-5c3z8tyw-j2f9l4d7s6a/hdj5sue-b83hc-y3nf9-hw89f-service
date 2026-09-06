using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Races.Command.UploadRaceMap;

public sealed class UploadRaceMapCommand : AuditedRequest, IRequest<string>
{
    public Guid RaceId { get; set; }
    public FileUploadModel File { get; set; } = null!;
}
