using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class StoreAppDetails
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("header_image")] public string? HeaderImage { get; set; }
        [JsonPropertyName("short_description")] public string? ShortDescription { get; set; }
        [JsonPropertyName("detailed_description")] public string? DetailedDescription { get; set; }
        [JsonPropertyName("about_the_game")] public string? AboutTheGame { get; set; }
        [JsonPropertyName("background")] public string? Background { get; set; }
        [JsonPropertyName("website")] public string? Website { get; set; }

        [JsonPropertyName("developers")] public string[]? Developers { get; set; }
        [JsonPropertyName("publishers")] public string[]? Publishers { get; set; }

        [JsonPropertyName("genres")] public List<StoreGenre>? Genres { get; set; }
    }
}


