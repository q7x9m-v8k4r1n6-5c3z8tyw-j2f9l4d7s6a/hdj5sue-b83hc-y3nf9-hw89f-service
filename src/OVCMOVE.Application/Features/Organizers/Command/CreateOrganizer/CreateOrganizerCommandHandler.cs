using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Organizers.Command.CreateOrganizer;

public class CreateOrganizerCommandHandler : IRequestHandler<CreateOrganizerCommand, OrganizerResponse>
{
    private readonly IOrganizerRepository _organizerRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrganizerCommandHandler> _logger;

    public CreateOrganizerCommandHandler(
        ILogger<CreateOrganizerCommandHandler> logger,
        IOrganizerRepository organizerRepo,
        IUserRepository userRepo,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _organizerRepo = organizerRepo;
        _userRepo = userRepo;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>Creates an organizer and assigns its initial role atomically.</summary>
    public async Task<OrganizerResponse> Handle(CreateOrganizerCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = CreateOrganizerFactory.NormalizeEmail(request.Email);
        var role = CreateOrganizerFactory.NormalizeRole(request.Role);
        var actor = request.GetActorOrSystem();

        if (await _organizerRepo.GetByEmailAsync(email, cancellationToken)
            is not null)
        {
            throw new ApplicationConflictException("Email đã được đăng ký.");
        }

        if (await _userRepo.GetByEmailAnyStatusAsync(email, cancellationToken)
            is not null)
        {
            throw new ApplicationConflictException(
                "Email đã được liên kết với một người dùng khác.");
        }

        var roleEntity = await _roleRepository.GetByCodeAsync(
            role,
            cancellationToken)
            ?? throw new ApplicationNotFoundException(
                $"Không tìm thấy role '{role}'.");
        var now = DateTime.UtcNow;
        var shortName = await ShortNameHelper.GenerateUniqueAsync(
            email,
            _userRepo,
            cancellationToken);
        var user = CreateOrganizerFactory.CreateUser(
            email,
            shortName,
            actor,
            now);

        // User and role assignment must either both succeed or both roll back.
        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await _userRepo.AddAsync(user, cancellationToken);
            await _userRoleRepository.CreateAsync(
                CreateOrganizerFactory.CreateUserRole(
                    user.Id,
                    roleEntity.Id,
                    actor,
                    now),
                cancellationToken);
            // Do not leave commit outcome ambiguous after a client disconnect.
            await _unitOfWork.CommitAsync(CancellationToken.None);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        await TrySendOrganizerCreatedEmailAsync(
            user,
            role,
            cancellationToken);
        return CreateOrganizerFactory.CreateResponse(user, role);
    }

    private async Task TrySendOrganizerCreatedEmailAsync(
        User user,
        string role,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = "OVCMOVE organizer account created";
            var body = $"""
                <p>Hello,</p>
                <p>Your OVCMOVE organizer account has been created.</p>
                <p><strong>Email:</strong> {user.LinkedEmail}</p>
                <p><strong>Role:</strong> {role}</p>
                <p><strong>Status:</strong> {user.Status}</p>
                """;

            await _emailService.SendOrganizerCredentialsAsync(
                user.LinkedEmail,
                subject,
                body,
                cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogWarning(
                exception,
                "Organizer account was created, but its notification email was canceled for {Email}.",
                user.LinkedEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Organizer account was created but email could not be sent to {Email}.", user.LinkedEmail);
        }
    }
}
