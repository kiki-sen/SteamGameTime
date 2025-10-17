using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class CurrentPlayersResponse
    {
        [JsonPropertyName("response")]
        public CurrentPlayersContainer? Response { get; set; }
    }
}


