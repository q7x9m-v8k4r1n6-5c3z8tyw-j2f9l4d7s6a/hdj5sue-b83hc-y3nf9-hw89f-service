namespace OVCMOVE.Api.Contracts;

public static class CommonContract
{
    public class PagedRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public sealed class PagedResponse<T>
    {
        public IReadOnlyCollection<T> Items { get; init; } = [];
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages { get; init; }
    }
}
