using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;

public class SearchOrganizerQueryHandler :
    BaseQueryHandler<SearchOrganizerQueryHandler>,
    IRequestHandler<SearchOrganizerQuery, List<SearchOrganizerResultModel>>
{
    private readonly IMapper _mapper;
    private readonly IOrganizerRepository _organizerRepository;

    public SearchOrganizerQueryHandler(
        ILogger<SearchOrganizerQueryHandler> logger,
        IMapper mapper,
        IOrganizerRepository organizerRepository) : base(logger)
    {
        _mapper = mapper;
        _organizerRepository = organizerRepository;
    }

    public async Task<List<SearchOrganizerResultModel>> Handle(SearchOrganizerQuery request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var organizers = await _organizerRepository.SearchAsync(request.Keyword, cancellationToken);
            return _mapper.Map<List<SearchOrganizerResultModel>>(organizers);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while handling SearchOrganizersQuery: {Message}", ex.Message);
            throw;
        }
    }
}