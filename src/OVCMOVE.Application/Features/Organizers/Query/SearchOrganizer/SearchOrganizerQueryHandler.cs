using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;

public class SearchOrganizerQueryHandler :
    IRequestHandler<SearchOrganizerQuery,
        IReadOnlyCollection<SearchOrganizerResultModel>>
{
    private readonly IOrganizerRepository _organizerRepository;

    public SearchOrganizerQueryHandler(
        IOrganizerRepository organizerRepository)
    {
        _organizerRepository = organizerRepository;
    }

    /// <summary>Searches organizer accounts and maps them to the feature result.</summary>
    public async Task<IReadOnlyCollection<SearchOrganizerResultModel>> Handle(
        SearchOrganizerQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ApplicationValidationException(
                "Từ khóa tìm kiếm không được để trống.");
        }

        var organizers = await _organizerRepository.SearchAsync(
            request.Keyword.Trim(),
            cancellationToken);
        return organizers.Select(MapOrganizer).ToArray();
    }

    private static SearchOrganizerResultModel MapOrganizer(User user) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName ?? string.Empty,
        Email = user.LinkedEmail,
        AvatarUrl = user.AvatarUrl
    };
}
