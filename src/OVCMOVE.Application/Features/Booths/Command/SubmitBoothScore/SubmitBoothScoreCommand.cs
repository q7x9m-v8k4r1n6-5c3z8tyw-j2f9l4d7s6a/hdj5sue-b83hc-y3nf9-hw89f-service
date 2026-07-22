using MediatR;


namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

public class SubmitBoothScoreCommand : IRequest<bool>
{
    public Guid BoothID { get; set; }
    public Guid TeamID { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public int Score { get; set; }
}