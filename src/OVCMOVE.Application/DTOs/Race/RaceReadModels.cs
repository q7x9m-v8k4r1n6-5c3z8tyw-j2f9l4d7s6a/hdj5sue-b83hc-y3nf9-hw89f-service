namespace OVCMOVE.Application.DTOs.Race;

public sealed class RaceBoothModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Place { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyCollection<Guid> OrganizerIds { get; init; } = [];
}

public sealed class RaceTeamModel
{
    public Guid TeamId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LeaderEmail { get; init; } = string.Empty;
}

public sealed class RaceOrganizerModel
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
