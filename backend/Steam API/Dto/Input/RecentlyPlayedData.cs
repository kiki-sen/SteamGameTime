using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class RecentlyPlayedData
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }
        [JsonPropertyName("games")]
        public List<RecentlyPlayedGame> Games { get; set; } = new();
    }
}


