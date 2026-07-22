using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
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
        IMapper mapper)
        : base(logger, mapper)
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

            var existing = await _organizerRepo.GetByEmailAsync(request.Email, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException("Email da duoc dang ky.");
            }

            var user = await _userRepo.GetByEmailAnyStatusAsync(request.Email, cancellationToken);
            if (user is null)
            {
                var now = DateTime.UtcNow;
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.Email,
                    Email = request.Email,
                    DisplayName = request.Email,
                    Role = UserConstant.Role.Organizer,
                    Status = UserConstant.Status.Active,
                    CreatedAt = now,
                    ModifiedAt = now
                };

                await _userRepo.AddAsync(user, cancellationToken);
            }
            else if (user.Role != UserConstant.Role.Organizer)
            {
                throw new InvalidOperationException("Email da ton tai voi role khac Organizer.");
            }

            var organizer = new Organizer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = user.DisplayName ?? user.Email,
                Email = user.Email,
                Role = user.Role,
                Status = OrganizerConstants.OrganizerStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _organizerRepo.AddAsync(organizer, cancellationToken);
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
