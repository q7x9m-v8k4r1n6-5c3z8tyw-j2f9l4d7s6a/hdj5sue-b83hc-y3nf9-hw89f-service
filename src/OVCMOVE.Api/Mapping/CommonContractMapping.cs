using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Api.Mapping;

public static class CommonContractMapping
{
    /// <summary>Maps an Application page while explicitly converting each item.</summary>
    public static CommonContract.PagedResponse<TResponse> ToResponse<
        TSource,
        TResponse>(
        this PagedResult<TSource> result,
        Func<TSource, TResponse> mapItem) => new()
        {
            Items = result.Items.Select(mapItem).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = result.TotalPages
        };
}
