namespace OVCMOVE.Api.Contracts;

public class TeamContract
{
    //Request đầu vào cho API View List
    public class GetTeamsRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    //Response đầu ra cho API View List
    public class GetTeamsResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    //Response đầu ra cho API Search
    public class SearchTeamResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}