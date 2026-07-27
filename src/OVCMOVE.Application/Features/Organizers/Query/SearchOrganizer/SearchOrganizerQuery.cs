using MediatR;

namespace OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;

public sealed record SearchOrganizerQuery(string Keyword)
    : IRequest<IReadOnlyCollection<SearchOrganizerResultModel>>;
