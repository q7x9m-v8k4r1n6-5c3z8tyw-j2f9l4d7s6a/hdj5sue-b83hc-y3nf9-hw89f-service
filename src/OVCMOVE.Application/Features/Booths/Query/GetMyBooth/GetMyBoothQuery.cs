using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Booths.Query.GetMyBooth;

public class GetMyBoothQuery : IRequest<MyBoothResultModel?>
{
    public Guid RaceId { get; set; }
    public Guid OrganizerId { get; set; }
}
