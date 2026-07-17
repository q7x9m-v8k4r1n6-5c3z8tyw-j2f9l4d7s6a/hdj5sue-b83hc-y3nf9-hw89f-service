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
    private readonly IEmailService _emailService;

    public CreateOrganizerHandler(
        ILogger<CreateOrganizerHandler> logger,
        IOrganizerRepository organizerRepo,
        IEmailService emailService,
        IMapper mapper)
        : base(logger, mapper)
    {
        _organizerRepo = organizerRepo;
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

            var organizer = new Organizer
            {
                Id = Guid.NewGuid(),
                DisplayName = request.Email,
                Email = request.Email,
                Role = request.Role,
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
