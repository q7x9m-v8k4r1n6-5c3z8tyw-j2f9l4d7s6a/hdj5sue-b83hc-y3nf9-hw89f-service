namespace OVCMOVE.Api.Contracts
{
    public class CommonContract
    {
        public class PagedRequest
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
        }
    }
}
