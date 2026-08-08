using MediatR;

namespace OVCMOVE2026.Plugin.CQRS.Commands.GenerateMissionQrCodes;

public class GenerateQrBatchResult
{
    public int TotalGenerated { get; set; }
    public int TotalFailed { get; set; }
}

public sealed record GenerateMissionQrCodesBatchCommand() : IRequest<GenerateQrBatchResult>;