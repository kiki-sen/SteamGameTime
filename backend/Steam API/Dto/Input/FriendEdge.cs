using System.Text.Json.Serialization;

namespace Steam_API.Dto.Input
{
    public sealed class FriendEdge 
    {
        [JsonPropertyName("steamid")] 
        public string Steamid { get; set; } = default!;

        [JsonPropertyName("friend_since")] 
        public long? FriendSince { get; set; } // unix seconds
    }
}


