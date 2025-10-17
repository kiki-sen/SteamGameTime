namespace Steam_API.Dto.Output
{
    public sealed class FriendsLeaderboardDto
    {
        public int? AppId { get; init; }                 
        public IReadOnlyList<FriendHoursRow> Rows { get; init; } = [];
    }
}


