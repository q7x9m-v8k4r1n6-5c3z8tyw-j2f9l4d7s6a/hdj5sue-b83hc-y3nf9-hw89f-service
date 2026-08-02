using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Booths.Query.GetMyBooth
{
    public class GetMyBoothQueryHandler : IRequestHandler<GetMyBoothQuery, Guid?>
    {
        private readonly IBoothOrganizerRepository _boothOrganizerRepository;
        private readonly IBoothRepository _boothRepository;

        public GetMyBoothQueryHandler(
            IBoothOrganizerRepository boothOrganizerRepository,
            IBoothRepository boothRepository)
        {
            _boothOrganizerRepository = boothOrganizerRepository;
            _boothRepository = boothRepository;
        }

        public async Task<Guid?> Handle(GetMyBoothQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _boothOrganizerRepository.GetByOrganizerAndRaceAsync(
                request.OrganizerId, request.RaceId, cancellationToken);

            return assignment?.BoothId;
        }
    }
}
