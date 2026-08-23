using System.Text.Json;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;
using OVCMOVE.Application.Features.Workflows.Common;

namespace OVCMOVE.Application.Features.Workflows.Command;

internal static class WorkflowTargetResolver
{
    public static IReadOnlyCollection<Guid> ResolveTeamIds(
        string target,
        WorkflowExecutionInputModel input,
        JsonElement config)
    {
        if (target == "actor" && input.ActorTeamId.HasValue)
            return [input.ActorTeamId.Value];
        if (target == "target" && input.TargetTeamId.HasValue)
            return [input.TargetTeamId.Value];
        if (target == "custom")
        {
            if (config.TryGetProperty("teamIds", out var teamIds) &&
                teamIds.ValueKind == JsonValueKind.Array)
            {
                var result = teamIds.EnumerateArray()
                    .Where(item =>
                        item.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(item.GetString(), out _))
                    .Select(item => Guid.Parse(item.GetString()!))
                    .Distinct()
                    .ToArray();
                if (result.Length > 0) return result;
            }
            if (config.TryGetProperty("teamId", out var teamId) &&
                teamId.ValueKind == JsonValueKind.String &&
                Guid.TryParse(teamId.GetString(), out var parsedTeamId))
                return [parsedTeamId];
        }
        throw new ApplicationValidationException(
            $"Sự kiện thiếu team cho target '{target}'.");
    }

    public static IReadOnlyCollection<RaceMessageRecipientModel> BuildRecipients(
        string target,
        WorkflowExecutionInputModel input,
        JsonElement config) => target switch
        {
            "all-teams" =>
            [
                new RaceMessageRecipientModel
                {
                    Key = "all-teams",
                    Label = "Tất cả team",
                    Type = "all-teams"
                }
            ],
            "actor" or "target" or "custom" => ResolveTeamIds(target, input, config)
                .Select(TeamRecipient)
                .ToArray(),
            _ => throw new ApplicationValidationException(
                "Đối tượng nhận thông báo không hợp lệ.")
        };

    private static RaceMessageRecipientModel TeamRecipient(Guid teamId) => new()
    {
        Key = $"team:{teamId:D}",
        Label = "Đội chơi",
        Type = "team"
    };
}
