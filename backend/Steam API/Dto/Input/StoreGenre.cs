using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class StoreGenre
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}


