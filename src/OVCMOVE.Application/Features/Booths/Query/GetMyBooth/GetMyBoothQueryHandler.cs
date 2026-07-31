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
            var assignment = await _boothOrganizerRepository.GetByOrganizerIdAsync(request.OrganizerId, cancellationToken);
            if (assignment is null) return null;

            var booth = await _boothRepository.GetByIdAsync(assignment.BoothId, cancellationToken);
            if (booth is null || booth.RaceId != request.RaceId) return null;

            return booth.Id;
        }
    }
}
