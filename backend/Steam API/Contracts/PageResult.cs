namespace Steam_API.Contracts
{
    public sealed class PageResult<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
        public required int Total { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
        public string? Sort { get; init; }
        public string? Q { get; init; }
    }
}
