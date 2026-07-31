using System.Net.Mail;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Organizers.Command.CreateOrganizer;

/// <summary>
/// Owns create-organizer validation and entity mapping so the handler only
/// orchestrates repositories and the transaction.
/// </summary>
internal static class CreateOrganizerFactory
{
    /// <summary>Validates and normalizes the organizer email.</summary>
    internal static string NormalizeEmail(string? email)
    {
        var normalizedEmail = email?.Trim() ?? string.Empty;
        if (normalizedEmail.Length > 320)
        {
            throw new ApplicationValidationException(
                "Email organizer không được vượt quá 320 ký tự.");
        }

        try
        {
            var address = new MailAddress(normalizedEmail);
            if (address.Address == normalizedEmail)
            {
                return normalizedEmail.ToLowerInvariant();
            }
        }
        catch (FormatException)
        {
            // Converted to the feature's stable validation exception below.
        }

        throw new ApplicationValidationException(
            "Email organizer không đúng định dạng.");
    }

    /// <summary>Creates the data-only user entity representing an organizer.</summary>
    internal static User CreateUser(
        string email,
        string shortName,
        string actor,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            LinkedEmail = email,
            UserType = UserConstants.UserType.Organizer,
            ShortName = shortName,
            Status = UserConstants.Status.Active,
            CreatedBy = actor,
            CreatedAt = now,
            ModifiedBy = actor,
            ModifiedAt = now,
            IsDeleted = false
        };

    /// <summary>Creates the relationship assigning a role to the new user.</summary>
    internal static UserRole CreateUserRole(
        Guid userId,
        Guid roleId,
        string actor,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor,
            IsDeleted = false
        };

    /// <summary>Maps the created user to the use-case response.</summary>
    internal static OrganizerResponse CreateResponse(User user, string role) =>
        new()
        {
            Id = user.Id,
            Email = user.LinkedEmail,
            DisplayName = user.DisplayName ?? string.Empty,
            Role = role,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
}
