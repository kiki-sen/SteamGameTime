using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class CurrentPlayersContainer
    {
        [JsonPropertyName("player_count")] 
        public int? PlayerCount { get; set; }

        [JsonPropertyName("result")] 
        public int? Result { get; set; } // 1 = OK
    }
}


