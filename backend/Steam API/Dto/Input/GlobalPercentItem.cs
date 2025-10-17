using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class GlobalPercentItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("percent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? Percent { get; set; }   // use double? to be extra safe
    }
}


