using MediatR;

namespace OVCMOVE.Application.Features.Booths.Query.GetMyBooth
{
    public class GetMyBoothQuery : IRequest<Guid?>
    {
        public Guid RaceId { get; set; }
        public Guid OrganizerId { get; set; }
    }
}
