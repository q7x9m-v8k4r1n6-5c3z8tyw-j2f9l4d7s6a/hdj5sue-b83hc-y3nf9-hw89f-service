namespace OVCMOVE.Domain.Entities;

public class Team
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
