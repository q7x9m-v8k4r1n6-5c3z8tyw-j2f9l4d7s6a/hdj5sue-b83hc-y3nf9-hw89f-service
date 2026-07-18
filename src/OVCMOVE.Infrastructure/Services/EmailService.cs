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
    {
        if (string.IsNullOrWhiteSpace(_emailConfig.Email) ||
            string.IsNullOrWhiteSpace(_emailConfig.Password))
        {
            throw new InvalidOperationException("Email service credentials are not configured.");
        }

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_emailConfig.Email, _emailConfig.Password)
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(_emailConfig.Email, "OVCMOVE Admin"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);

        await client.SendMailAsync(mail, cancellationToken);
    }
}
