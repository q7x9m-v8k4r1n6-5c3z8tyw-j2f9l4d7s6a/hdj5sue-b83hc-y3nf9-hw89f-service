using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ExternalServicesConfigOptions.EmailServiceOption _emailConfig;

    public EmailService(IOptions<ExternalServicesConfigOptions> options)
    {
        _emailConfig = options.Value.EmailService
            ?? throw new InvalidOperationException("Email service configuration is not configured.");
    }

    public async Task SendOrganizerCredentialsAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        => await SendAsync(toEmail, subject, body, cancellationToken);

    public async Task SendTeamCredentialsAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        => await SendAsync(toEmail, subject, body, cancellationToken);

    private async Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_emailConfig.Email) ||
            string.IsNullOrWhiteSpace(_emailConfig.Password))
        {
            throw new InvalidOperationException("Email service credentials are not configured.");
        }

        var senderEmail = _emailConfig.Email.Trim();
        if (!MailAddress.TryCreate(senderEmail, out var senderAddress) ||
            !string.Equals(senderAddress.Address, senderEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ExternalServicesConfig:EmailService:Email phải là một địa chỉ Gmail hợp lệ.");
        }

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(senderEmail, _emailConfig.Password)
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(senderAddress.Address, "OVC MOVE"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);

        await client.SendMailAsync(mail, cancellationToken);
    }
}
