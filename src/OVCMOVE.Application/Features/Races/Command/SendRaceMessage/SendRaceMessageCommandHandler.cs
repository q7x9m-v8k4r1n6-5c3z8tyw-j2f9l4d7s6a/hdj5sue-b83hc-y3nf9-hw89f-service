using System.Text.Json;
using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
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
        var body = ValidateBody(request);

        var race = await _raceRepository.GetDetailAsync(request.RaceId, cancellationToken);
        if (race is null) return null;

        var recipients = NormalizeRecipients(request, race);
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

        if (!_unitOfWork.HasActiveTransaction &&
            request.PublishRealtimeNotification)
        {
            await _notificationService.NotifyRaceMessageAsync(
                request.RaceId,
                result,
                cancellationToken);
        }

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
        SendRaceMessageCommand request,
        RaceDetailResultModel race)
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

        return EnsureRecipientsBelongToRace(recipients, race);
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

    private static IReadOnlyCollection<RaceMessageRecipientModel> EnsureRecipientsBelongToRace(
        IReadOnlyCollection<RaceMessageRecipientModel> recipients,
        RaceDetailResultModel race)
    {
        var teamsByKey = race.RaceTeam.ToDictionary(
            team => $"{RaceMessageRecipientConstants.TeamKeyPrefix}{team.TeamId:D}",
            team => string.IsNullOrWhiteSpace(team.Name)
                ? team.LeaderEmail
                : team.Name,
            StringComparer.OrdinalIgnoreCase);
        var organizersByKey = race.Organizers
            .Concat(race.OrganizerId.Select(id => new RaceOrganizerModel { Id = id }))
            .GroupBy(
                organizer => $"{RaceMessageRecipientConstants.OrganizerKeyPrefix}{organizer.Id:D}",
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var organizer = group.FirstOrDefault(item =>
                        !string.IsNullOrWhiteSpace(item.DisplayName) ||
                        !string.IsNullOrWhiteSpace(item.Email));
                    return string.IsNullOrWhiteSpace(organizer?.DisplayName)
                        ? organizer?.Email ?? "Ban tổ chức"
                        : organizer.DisplayName;
                },
                StringComparer.OrdinalIgnoreCase);

        return recipients.Select(recipient => recipient.Type switch
        {
            RaceMessageRecipientConstants.All => WithLabel(
                recipient,
                "Tất cả mọi người"),
            RaceMessageRecipientConstants.AllTeams => WithLabel(
                recipient,
                "Tất cả team"),
            RaceMessageRecipientConstants.AllOrganizers => WithLabel(
                recipient,
                "Tất cả ban tổ chức"),
            RaceMessageRecipientConstants.Team => WithLabel(
                recipient,
                GetAssignedRecipientLabel(teamsByKey, recipient.Key)),
            RaceMessageRecipientConstants.Organizer => WithLabel(
                recipient,
                GetAssignedRecipientLabel(organizersByKey, recipient.Key)),
            _ => throw new ApplicationValidationException("Recipient type is invalid.")
        }).ToArray();

        static RaceMessageRecipientModel WithLabel(
            RaceMessageRecipientModel recipient,
            string label) => new()
            {
                Key = recipient.Key,
                Label = label,
                Type = recipient.Type
            };
    }

    private static string GetAssignedRecipientLabel(
        IReadOnlyDictionary<string, string> recipientsByKey,
        string key)
    {
        if (!recipientsByKey.TryGetValue(key, out var label))
        {
            throw new ApplicationValidationException("Recipient is not assigned to this race.");
        }

        return string.IsNullOrWhiteSpace(label)
            ? "Người nhận"
            : label;
    }
}
