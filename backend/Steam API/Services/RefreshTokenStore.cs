using Steam_API.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Steam_API.Services
{
    public interface IRefreshTokenStore
    {
        string CreateRefreshToken(string steamId);
        RefreshToken? GetRefreshToken(string token);
        void RevokeRefreshToken(string token);
        void CleanupExpiredTokens();
    }

    public class InMemoryRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();
        private readonly IConfiguration _config;

        public InMemoryRefreshTokenStore(IConfiguration config)
        {
            _config = config;
        }

        public string CreateRefreshToken(string steamId)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes);

            var refreshTokenLifetimeDays = _config.GetValue<int>("Jwt:RefreshTokenLifetimeDays", 30);

            var refreshToken = new RefreshToken
            {
                Token = token,
                SteamId = steamId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenLifetimeDays)
            };

            _tokens[token] = refreshToken;

            CleanupExpiredTokens();

            return token;
        }

        public RefreshToken? GetRefreshToken(string token)
        {
            if (_tokens.TryGetValue(token, out var refreshToken))
            {
                return refreshToken.IsExpired ? null : refreshToken;
            }

            return null;
        }

        public void RevokeRefreshToken(string token)
        {
            _tokens.TryRemove(token, out _);
        }

        public void CleanupExpiredTokens()
        {
            var expiredTokens = _tokens.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();

            foreach (var token in expiredTokens)
            {
                _tokens.TryRemove(token, out _);
            }
        }
    }
}
