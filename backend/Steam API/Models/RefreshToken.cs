namespace Steam_API.Models
{
    public class RefreshToken
    {
        public required string Token { get; init; }
        public required string SteamId { get; init; }
        public DateTime ExpiresAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
