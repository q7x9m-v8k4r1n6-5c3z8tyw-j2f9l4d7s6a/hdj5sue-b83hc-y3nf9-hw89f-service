using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;
namespace OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;

public class SearchOrganizerQuery : IRequest<List<SearchOrganizerResultModel>>
{
    public string Keyword { get; set; } = string.Empty;

    public SearchOrganizerQuery(string keyword)
    {
        Keyword = keyword;
    }
}