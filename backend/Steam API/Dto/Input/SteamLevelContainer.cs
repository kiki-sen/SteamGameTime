using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class SteamLevelContainer 
    {
        [JsonPropertyName("player_level")]
        public int? PlayerLevel { get; set; } 
    }
}


