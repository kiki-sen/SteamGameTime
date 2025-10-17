using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class RecentlyPlayedGame
    {
        [JsonPropertyName("appid")]
        public uint AppId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        // minutes in the last 2 weeks (nullable when not played recently)
        [JsonPropertyName("playtime_2weeks")]
        public int? Playtime2Weeks { get; set; }

        // total minutes on record
        [JsonPropertyName("playtime_forever")]
        public int PlaytimeForever { get; set; }
    }
}


