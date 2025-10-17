using System.ComponentModel.DataAnnotations;

namespace Steam_API.Contracts
{
    public sealed class GamesQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 10000)]
        public int PageSize { get; init; } = 25;

        // e.g., "hoursTotal:desc", "name", "hours2w:asc"
        public string? Sort { get; init; }

        // search by name (case-insensitive, contains)
        [MaxLength(100)]
        public string? Q { get; init; }
    }
}
