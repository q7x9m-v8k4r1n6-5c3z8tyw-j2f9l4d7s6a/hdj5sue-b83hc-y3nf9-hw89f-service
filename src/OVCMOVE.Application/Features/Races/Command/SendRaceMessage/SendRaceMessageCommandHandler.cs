using System.Text.Json;
using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Races.Command.SendRaceMessage;

public sealed class SendRaceMessageCommandHandler :
    IRequestHandler<SendRaceMessageCommand, RaceMessageResultModel?>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public SendRaceMessageCommandHandler(
        IRaceRepository raceRepository,
        IBoothNotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _raceRepository = raceRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RaceMessageResultModel?> Handle(
        SendRaceMessageCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(request);

        var raceExists = await _raceRepository.ExistsAsync(request.RaceId, cancellationToken);
        if (!raceExists) return null;

        var actor = request.GetActorOrSystem();
        var senderName = actor;
        var now = DateTime.UtcNow;
        var recipients = request.Recipients
            .Select(recipient => new RaceMessageRecipientModel
            {
                Key = recipient.Key.Trim(),
                Label = recipient.Label.Trim(),
                Type = recipient.Type.Trim()
            })
            .ToArray();
        var recipientKeys = recipients.Select(recipient => recipient.Key).ToArray();
        var recipientLabels = recipients.Select(recipient => recipient.Label).ToArray();
        var body = request.Body.Trim();
        var message = new RaceMessage
        {
            Id = Guid.NewGuid(),
            RaceId = request.RaceId,
            SenderId = request.SenderId,
            SenderName = senderName,
            RecipientKeysJson = JsonSerializer.Serialize(recipientKeys),
            RecipientLabelsJson = JsonSerializer.Serialize(recipientLabels),
            Body = body,
            CreatedBy = actor,
            CreatedAt = now,
            ModifiedBy = actor,
            ModifiedAt = now,
            IsDeleted = false
        };

        try
        {
            await _unitOfWork.BeginAsync(cancellationToken);
            await _raceRepository.CreateRaceMessageAsync(message, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        var result = new RaceMessageResultModel
        {
            Id = message.Id,
            RaceId = message.RaceId,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            RecipientKeys = recipientKeys,
            RecipientLabels = recipientLabels,
            Body = message.Body,
            CreatedAt = message.CreatedAt
        };

        await _notificationService.NotifyRaceMessageAsync(
            request.RaceId,
            result,
            cancellationToken);

        return result;
    }

    private static void Validate(SendRaceMessageCommand request)
    {
        if (request.RaceId == Guid.Empty)
        {
            throw new ApplicationValidationException("RaceId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ApplicationValidationException("Message body is required.");
        }

        if (request.Recipients.Count == 0)
        {
            throw new ApplicationValidationException("At least one recipient is required.");
        }

        if (request.Recipients.Any(recipient =>
                string.IsNullOrWhiteSpace(recipient.Key) ||
                string.IsNullOrWhiteSpace(recipient.Label) ||
                string.IsNullOrWhiteSpace(recipient.Type)))
        {
            throw new ApplicationValidationException("Recipient is invalid.");
        }
    }
}
