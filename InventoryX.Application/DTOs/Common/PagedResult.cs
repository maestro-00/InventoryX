namespace InventoryX.Application.DTOs.Common
{
    /// <summary>Standard paged list envelope for all list endpoints (research R10).</summary>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];
        public int Page { get; init; }
        public int PageSize { get; init; }
        public long TotalCount { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNext => Page < TotalPages;
        public bool HasPrevious => Page > 1;

        public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, long totalCount) =>
            new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    /// <summary>Pagination binding parameters with clamped defaults (default 50, max 200).</summary>
    public record PageRequest
    {
        public const int DefaultPageSize = 50;
        public const int MaxPageSize = 200;

        private readonly int _page = 1;
        private readonly int _pageSize = DefaultPageSize;

        public int Page
        {
            get => _page;
            init => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            init => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
        }

        public int Skip => (Page - 1) * PageSize;
    }
}
