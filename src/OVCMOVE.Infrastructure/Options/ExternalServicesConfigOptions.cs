namespace OVCMOVE.Infrastructure.Options;

public class ExternalServicesConfigOptions
{
    public const string SectionName = "ExternalServicesConfig";
    public EmailServiceOption EmailService { get; set; } = new();

    public class EmailServiceOption
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
