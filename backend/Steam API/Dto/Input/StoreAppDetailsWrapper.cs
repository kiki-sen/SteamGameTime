using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class StoreAppDetailsWrapper
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public StoreAppDetails? Data { get; set; }
    }
}


