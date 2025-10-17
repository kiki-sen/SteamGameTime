namespace Steam_API.Dto.Output
{
    public sealed class FriendsListDto
    {
        public IReadOnlyList<FriendSummaryDto> Rows { get; init; } = [];
    }
}


