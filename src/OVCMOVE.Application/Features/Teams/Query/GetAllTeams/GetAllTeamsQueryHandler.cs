using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.GetAllTeams;

public class GetAllTeamsQueryHandler :
    BaseQueryHandler<GetAllTeamsQueryHandler>,
    IRequestHandler<GetAllTeamsQuery, PagedResult<GetAllTeamsResultModel>>
{
    private readonly IMapper _mapper;
    private readonly ITeamRepository _teamRepository;

    public GetAllTeamsQueryHandler(
        ILogger<GetAllTeamsQueryHandler> logger,
        IMapper mapper,
        ITeamRepository teamRepository) : base(logger)
    {
        _mapper = mapper;
        _teamRepository = teamRepository;
    }

    public async Task<PagedResult<GetAllTeamsResultModel>> Handle(GetAllTeamsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Lấy toàn bộ danh sách từ Repo 
            var allTeams = await _teamRepository.GetAllAsync(cancellationToken);
            var totalItems = allTeams.Count;

            // 2. Cắt mảng lấy đúng 20 items dựa theo số trang 
            var pagedTeams = allTeams
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var mappedItems = _mapper.Map<List<GetAllTeamsResultModel>>(pagedTeams);

            // 3. Đóng gói vào khuôn PagedResult
            return new PagedResult<GetAllTeamsResultModel>
            {
                Items = mappedItems,
                TotalItems = totalItems,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while handling GetAllTeamsQuery: {Message}", ex.Message);
            throw;
        }
    }
}