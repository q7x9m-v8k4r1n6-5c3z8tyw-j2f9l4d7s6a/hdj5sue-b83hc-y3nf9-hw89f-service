using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;

namespace OVCMOVE.API.Hubs;

internal static class RaceMessageHubGroups
{
    public static string All(Guid raceId) => $"RaceMessage_{raceId}:all";

    public static string AllTeams(Guid raceId) => $"RaceMessage_{raceId}:all-teams";

    public static string AllOrganizers(Guid raceId) => $"RaceMessage_{raceId}:all-organizers";

    public static string Team(Guid raceId, Guid teamId) => $"RaceMessage_{raceId}:team:{teamId}";

    public static string Organizer(Guid raceId, Guid organizerId) => $"RaceMessage_{raceId}:organizer:{organizerId}";

    public static IReadOnlyCollection<string> FromRecipientKeys(
        Guid raceId,
        IEnumerable<string> recipientKeys)
    {
        var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in recipientKeys.Select(item => item.Trim()))
        {
            if (string.Equals(key, RaceMessageRecipientConstants.All, StringComparison.OrdinalIgnoreCase))
            {
                groups.Add(All(raceId));
                continue;
            }

            if (string.Equals(key, RaceMessageRecipientConstants.AllTeams, StringComparison.OrdinalIgnoreCase))
            {
                groups.Add(AllTeams(raceId));
                continue;
            }

            if (string.Equals(key, RaceMessageRecipientConstants.AllOrganizers, StringComparison.OrdinalIgnoreCase))
            {
                groups.Add(AllOrganizers(raceId));
                continue;
            }

            if (TryParseScopedRecipient(key, RaceMessageRecipientConstants.TeamKeyPrefix, out var teamId))
            {
                groups.Add(Team(raceId, teamId));
                continue;
            }

            if (TryParseScopedRecipient(key, RaceMessageRecipientConstants.OrganizerKeyPrefix, out var organizerId))
            {
                groups.Add(Organizer(raceId, organizerId));
            }
        }

        return groups.ToArray();
    }

    private static bool TryParseScopedRecipient(
        string key,
        string prefix,
        out Guid id)
    {
        id = Guid.Empty;
        return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(key[prefix.Length..], out id);
    }
}
