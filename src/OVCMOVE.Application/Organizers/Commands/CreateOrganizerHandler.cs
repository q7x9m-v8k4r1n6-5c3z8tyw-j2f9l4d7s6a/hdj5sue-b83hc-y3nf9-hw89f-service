using System.Net;
using AutoMapper;
using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.DTOs.Organizer;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Organizers.Commands;

public class CreateOrganizerHandler : IRequestHandler<CreateOrganizerCommand, OrganizerResponse>
{
    private readonly IOrganizerRepository _organizerRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public CreateOrganizerHandler(
        IOrganizerRepository organizerRepo,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        IEmailService emailService)
    {
        _organizerRepo = organizerRepo;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _emailService = emailService;
    }

    public async Task<OrganizerResponse> Handle(CreateOrganizerCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra email đã tồn tại chưa
        var existing = await _organizerRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email đã được đăng ký.");

        // 2. Hash mật khẩu
        var passwordHash = _passwordHasher.Hash(request.Password);

        // 3. Tạo entity Organizer
        var organizer = new Organizer
        {
            Id = Guid.NewGuid(),
            Name = request.FullName,
            Username = request.Email,
            Email = request.Email,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Lưu vào DB
        await _organizerRepo.AddAsync(organizer);

        var subject = "Tai khoan Organizer OVCMOVE";
        var fullName = WebUtility.HtmlEncode(request.FullName);
        var email = WebUtility.HtmlEncode(request.Email);
        var password = WebUtility.HtmlEncode(request.Password);
        var body = $@"
            <h3>Xin chao {fullName},</h3>
            <p>Tai khoan Organizer cua ban da duoc tao thanh cong.</p>
            <p><strong>Email:</strong> {email}</p>
            <p><strong>Mat khau:</strong> {password}</p>
            <p>Vui long dang nhap tai <a href='https://localhost:7093/login'>day</a>.</p>
            <p>Tran trong,<br/>OVCMOVE Team</p>";

        try
        {
            await _emailService.SendOrganizerCredentialsAsync(request.Email, subject, body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send organizer credentials email to {request.Email}: {ex.Message}");
        }

        // 5. Map sang DTO response
        return _mapper.Map<OrganizerResponse>(organizer);
    }
}
