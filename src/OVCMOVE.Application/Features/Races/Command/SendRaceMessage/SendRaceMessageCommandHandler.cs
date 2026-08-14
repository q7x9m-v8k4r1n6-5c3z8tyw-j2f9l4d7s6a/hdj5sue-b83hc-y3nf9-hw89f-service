using System.Text.Json;
using MediatR;
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

    public SendRaceMessageCommandHandler(
        IRaceRepository raceRepository,
        IBoothNotificationService notificationService)
    {
        _raceRepository = raceRepository;
        _notificationService = notificationService;
    }

    public async Task<RaceMessageResultModel?> Handle(
        SendRaceMessageCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var body = ValidateBody(request);
        var recipients = NormalizeRecipients(request);

        var race = await _raceRepository.GetByIdAsync(request.RaceId, cancellationToken);
        if (race is null) return null;

        var actor = request.GetActorOrSystem();
        var senderName = actor;
        var now = DateTime.UtcNow;
        var recipientKeys = recipients.Select(recipient => recipient.Key).ToArray();
        var recipientLabels = recipients.Select(recipient => recipient.Label).ToArray();
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

        await _raceRepository.CreateRaceMessageAsync(message, cancellationToken);

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

    private static string ValidateBody(SendRaceMessageCommand request)
    {
        if (request.RaceId == Guid.Empty)
        {
            throw new ApplicationValidationException("RaceId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ApplicationValidationException("Message body is required.");
        }

        return request.Body.Trim();
    }

    private static IReadOnlyCollection<RaceMessageRecipientModel> NormalizeRecipients(
        SendRaceMessageCommand request)
    {
        if (request.Recipients.Count == 0)
        {
            throw new ApplicationValidationException("At least one recipient is required.");
        }

        var recipients = request.Recipients
            .Select(NormalizeRecipient)
            .DistinctBy(recipient => recipient.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (recipients.Length == 0)
        {
            throw new ApplicationValidationException("Recipient is invalid.");
        }

        return recipients;
    }

    private static RaceMessageRecipientModel NormalizeRecipient(
        RaceMessageRecipientModel recipient)
    {
        var type = recipient.Type.Trim().ToLowerInvariant();
        var key = recipient.Key.Trim().ToLowerInvariant();
        var label = recipient.Label.Trim();

        if (string.IsNullOrWhiteSpace(key) ||
            string.IsNullOrWhiteSpace(label) ||
            string.IsNullOrWhiteSpace(type))
        {
            throw new ApplicationValidationException("Recipient is invalid.");
        }

        var normalizedKey = type switch
        {
            RaceMessageRecipientConstants.All =>
                RequireExactKey(key, RaceMessageRecipientConstants.All),
            RaceMessageRecipientConstants.AllTeams =>
                RequireExactKey(key, RaceMessageRecipientConstants.AllTeams),
            RaceMessageRecipientConstants.AllOrganizers =>
                RequireExactKey(key, RaceMessageRecipientConstants.AllOrganizers),
            RaceMessageRecipientConstants.Team =>
                RequireScopedKey(key, RaceMessageRecipientConstants.TeamKeyPrefix),
            RaceMessageRecipientConstants.Organizer =>
                RequireScopedKey(key, RaceMessageRecipientConstants.OrganizerKeyPrefix),
            _ => throw new ApplicationValidationException("Recipient type is invalid.")
        };

        return new RaceMessageRecipientModel
        {
            Key = normalizedKey,
            Label = label,
            Type = type
        };
    }

    private static string RequireExactKey(string key, string expectedKey)
    {
        if (!string.Equals(key, expectedKey, StringComparison.Ordinal))
        {
            throw new ApplicationValidationException("Recipient key is invalid.");
        }

        return expectedKey;
    }

    private static string RequireScopedKey(string key, string prefix)
    {
        if (!key.StartsWith(prefix, StringComparison.Ordinal) ||
            !Guid.TryParse(key[prefix.Length..], out var id))
        {
            throw new ApplicationValidationException("Recipient key is invalid.");
        }

        return $"{prefix}{id:D}";
    }
}
