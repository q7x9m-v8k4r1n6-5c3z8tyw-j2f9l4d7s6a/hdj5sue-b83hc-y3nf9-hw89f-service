namespace OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;

public class SearchOrganizerResultModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}