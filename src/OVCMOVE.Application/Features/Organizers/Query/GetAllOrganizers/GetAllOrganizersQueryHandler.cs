using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;

// Đổi kiểu đầu ra ở Interface thành PagedResult
public class GetAllOrganizersQueryHandler :
    BaseQueryHandler<GetAllOrganizersQueryHandler>,
    IRequestHandler<GetAllOrganizersQuery, PagedResult<GetAllOrganizersResultModel>>
{
    private readonly IMapper _mapper;
    private readonly IOrganizerRepository _organizerRepository; 

    public GetAllOrganizersQueryHandler(
        ILogger<GetAllOrganizersQueryHandler> logger,
        IMapper mapper,
        IOrganizerRepository organizerRepository) : base(logger)
    {
        _mapper = mapper;
        _organizerRepository = organizerRepository;
    }

    public async Task<PagedResult<GetAllOrganizersResultModel>> Handle(GetAllOrganizersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Lấy toàn bộ danh sách từ Database lên qua Repository
            var allOrganizers = await _organizerRepository.GetAllAsync(cancellationToken);
            var totalItems = allOrganizers.Count;

            // 2. Cắt đúng 20 dòng dựa theo số trang hiện tại (Phân trang logic)
            var pagedOrganizers = allOrganizers
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var mappedItems = _mapper.Map<List<GetAllOrganizersResultModel>>(pagedOrganizers);

            return new PagedResult<GetAllOrganizersResultModel>
            {
                Items = mappedItems,
                TotalItems = totalItems,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while handling GetAllOrganizersQuery: {Message}", ex.Message);
            throw;
        }
    }
}