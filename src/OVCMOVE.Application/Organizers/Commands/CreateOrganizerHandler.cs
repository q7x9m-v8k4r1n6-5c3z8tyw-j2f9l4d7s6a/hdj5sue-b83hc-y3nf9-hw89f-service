using System.Net.Mail;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Organizer;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Organizers.Commands;

public class CreateOrganizerHandler : BaseCommandHandler<CreateOrganizerHandler>, IRequestHandler<CreateOrganizerCommand, OrganizerResponse>
{
    private readonly IOrganizerRepository _organizerRepo;
    private readonly IUserRepository _userRepo;
    private readonly IEmailService _emailService;

    public CreateOrganizerHandler(
        ILogger<CreateOrganizerHandler> logger,
        IOrganizerRepository organizerRepo,
        IUserRepository userRepo,
        IEmailService emailService,
        IMapper mapper,
        IUnitOfWork unitOfWork)
        : base(logger, mapper, unitOfWork)
    {
        _organizerRepo = organizerRepo;
        _userRepo = userRepo;
        _emailService = emailService;
    }

    public async Task<OrganizerResponse> Handle(CreateOrganizerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var email = request.Email?.Trim() ?? string.Empty;
            if (!IsValidEmail(email))
            {
                throw new InvalidOperationException("Invalid organizer email format.");
            }

            var role = NormalizeRole(request.Role);

            var existing = await _organizerRepo.GetByEmailAsync(email, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException("Email da duoc dang ky.");
            }

            var user = await _userRepo.GetByEmailAnyStatusAsync(email, cancellationToken);
            var isNewUser = user is null;
            if (user is null)
            {
                var now = DateTime.UtcNow;
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = null,
                    Email = email,
                    DisplayName = null,
                    Role = role,
                    Status = UserConstant.Status.Active,
                    CreatedAt = now,
                    ModifiedAt = now
                };
            }
            else if (!string.Equals(user.Role, role, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Email da ton tai voi role khac.");
            }

            var organizer = new Organizer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = user.DisplayName ?? string.Empty,
                Email = user.Email,
                Role = user.Role,
                Status = OrganizerConstants.OrganizerStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var unitOfWork = _unitOfWork
                ?? throw new InvalidOperationException("Unit of work is not configured.");

            unitOfWork.Begin();
            try
            {
                if (isNewUser)
                {
                    await _userRepo.AddAsync(user, cancellationToken);
                }

                await _organizerRepo.AddAsync(organizer, cancellationToken);
                unitOfWork.Commit();
            }
            catch
            {
                unitOfWork.Rollback();
                throw;
            }

            await TrySendOrganizerCreatedEmailAsync(organizer, cancellationToken);

            return _mapper.Map<OrganizerResponse>(organizer);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling CreateOrganizerCommand for {Email}.", request.Email);
            throw;
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeRole(string role)
    {
        return (role ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            UserConstant.Role.Organizer => UserConstant.Role.Organizer,
            UserConstant.Role.Admin => UserConstant.Role.Admin,
            "administrator" => UserConstant.Role.Admin,
            _ => throw new InvalidOperationException("Role must be Organizer or Administrator.")
        };
    }

    private async Task TrySendOrganizerCreatedEmailAsync(Organizer organizer, CancellationToken cancellationToken)
    {
        try
        {
            var subject = "OVCMOVE organizer account created";
            var body = $"""
                <p>Hello,</p>
                <p>Your OVCMOVE organizer account has been created.</p>
                <p><strong>Email:</strong> {organizer.Email}</p>
                <p><strong>Role:</strong> {organizer.Role}</p>
                <p><strong>Status:</strong> {organizer.Status}</p>
                """;

            await _emailService.SendOrganizerCredentialsAsync(
                organizer.Email,
                subject,
                body,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Organizer account was created but email could not be sent to {Email}.", organizer.Email);
        }
    }
}
