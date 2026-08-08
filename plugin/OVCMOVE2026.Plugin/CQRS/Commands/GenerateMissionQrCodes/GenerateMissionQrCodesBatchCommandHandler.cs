using MediatR;
using Microsoft.Extensions.Configuration;

using OVCMOVE.Application.Abstractions;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services.QrCode;

namespace OVCMOVE2026.Plugin.CQRS.Commands.GenerateMissionQrCodes;

public class GenerateMissionQrCodesBatchCommandHandler : IRequestHandler<GenerateMissionQrCodesBatchCommand, GenerateQrBatchResult>
{
    private readonly ISecretMissionRepository _repository;
    private readonly IQrCodeGeneratorService _qrCodeService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;

    public GenerateMissionQrCodesBatchCommandHandler(
        ISecretMissionRepository repository,
        IQrCodeGeneratorService qrCodeService,
        IBlobStorageService blobStorageService,
        IConfiguration configuration)
    {
        _repository = repository;
        _qrCodeService = qrCodeService;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
    }

    public async Task<GenerateQrBatchResult> Handle(GenerateMissionQrCodesBatchCommand request, CancellationToken cancellationToken)
    {
        var result = new GenerateQrBatchResult();
        
        var missions = await _repository.GetMissionsWithoutQrCodeAsync(cancellationToken);
        
        var missionsList = missions.ToList();
        if (!missionsList.Any())
        {
            return result;
        }

        var qrContainer = _configuration["OVCMOVE_AzureBlobStorage:QrContainerName"] ?? "mission-qrcodes";

        foreach (var mission in missionsList)
        {
            try
            {
                var qrPayload = mission.Id.ToString();
                var pngBytes = _qrCodeService.GeneratePngBytes(qrPayload);

                using var stream = new MemoryStream(pngBytes);
                var fileName = $"qr_{mission.Id}.png"; // VD: qr_2222...png
                
                var blobUrl = await _blobStorageService.UploadAsync(
                    stream, 
                    fileName, 
                    "image/png", 
                    qrContainer, 
                    cancellationToken);

                await _repository.UpdateQrCodeUrlAsync(mission.Id, blobUrl, cancellationToken);

                result.TotalGenerated++;
            }
            catch
            {
                result.TotalFailed++;
            }
        }

        return result;
    }
}