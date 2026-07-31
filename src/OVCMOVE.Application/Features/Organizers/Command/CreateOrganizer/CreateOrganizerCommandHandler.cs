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
        var roleIds = request.RoleIds.Distinct().ToArray();
        if (roleIds.Length == 0)
        {
            throw new ApplicationValidationException("Vui lòng chọn ít nhất một vai trò.");
        }
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

        var roles = new List<Domain.Entities.Role>();
        foreach (var roleId in roleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
                ?? throw new ApplicationNotFoundException("Vai trò được chọn không tồn tại.");
            roles.Add(role);
        }
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
            foreach (var role in roles)
            {
                await _userRoleRepository.CreateAsync(
                    CreateOrganizerFactory.CreateUserRole(user.Id, role.Id, actor, now),
                    cancellationToken);
            }
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
            string.Join(", ", roles.Select(item => item.Name)),
            cancellationToken);
        return CreateOrganizerFactory.CreateResponse(
            user,
            string.Join(", ", roles.Select(item => item.Code)));
    }

    private async Task TrySendOrganizerCreatedEmailAsync(
        User user,
        string role,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = AccountEmailTemplate.Subject("Tài khoản Ban tổ chức MOVE đã được tạo");
            var body = AccountEmailTemplate.Build(
                "Tài khoản Ban tổ chức đã sẵn sàng",
                user.DisplayName ?? user.LinkedEmail,
                "tài khoản MOVE của bạn đã được tạo thành công.",
                [("Email", user.LinkedEmail), ("Vai trò", role), ("Trạng thái", user.Status)]);

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
