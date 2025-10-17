namespace Steam_API.Dto.Output
{
    public sealed class FriendSummaryDto
    {
        public string SteamId64 { get; init; } = default!;
        public string PersonaName { get; init; } = "";
        public string? AvatarSmall { get; init; }
        public string? AvatarMedium { get; init; }
        public string? AvatarFull { get; init; }
        public int? PersonaState { get; init; }              // 0..6
        public int CommunityVisibilityState { get; init; }   // 1=Private, 3=Public
        public DateTimeOffset? FriendSinceUtc { get; init; } // when you became friends (if available)
        public bool IsYou { get; init; }
        public int? SteamLevel { get; set; }
        public string? GameId { get; init; }      // currently playing game ID
        public string? GameName { get; init; }    // currently playing game name
    }
}


