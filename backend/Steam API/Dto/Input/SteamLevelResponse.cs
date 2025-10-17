using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class SteamLevelResponse 
    {
        [JsonPropertyName("response")]
        public SteamLevelContainer? Response { get; set; } 
    }
}

