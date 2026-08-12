using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Common;

namespace OVCMOVE.Application.Features.Races.Command.SendRaceMessage;

public sealed class SendRaceMessageCommand :
    AuditedRequest,
    IRequest<RaceMessageResultModel?>
{
    public Guid RaceId { get; init; }
    public Guid? SenderId { get; init; }
    public IReadOnlyCollection<RaceMessageRecipientModel> Recipients { get; init; } = [];
    public string Body { get; init; } = string.Empty;
}

public sealed class RaceMessageRecipientModel
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}
