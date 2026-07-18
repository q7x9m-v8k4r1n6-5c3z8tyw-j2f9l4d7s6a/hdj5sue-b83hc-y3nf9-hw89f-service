namespace OVCMOVE.Application.Abstractions.Services;

public interface IEmailService
{
    Task SendOrganizerCredentialsAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
