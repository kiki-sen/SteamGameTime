using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Steam_API.Services;
using System.Collections.Generic;

namespace Steam_API_Tests.Services
{
    public class RefreshTokenStoreTests
    {
        private readonly IConfiguration _config;
        private readonly InMemoryRefreshTokenStore _store;

        public RefreshTokenStoreTests()
        {
            var configValues = new Dictionary<string, string>
            {
                ["Jwt:RefreshTokenLifetimeDays"] = "30"
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues!)
                .Build();

            _store = new InMemoryRefreshTokenStore(_config);
        }

        [Fact]
        public void CreateRefreshToken_GeneratesUniqueToken()
        {
            // Arrange
            var steamId = "76561198000000000";

            // Act
            var token1 = _store.CreateRefreshToken(steamId);
            var token2 = _store.CreateRefreshToken(steamId);

            // Assert
            token1.Should().NotBeNullOrEmpty();
            token2.Should().NotBeNullOrEmpty();
            token1.Should().NotBe(token2);
        }

        [Fact]
        public void GetRefreshToken_WithValidToken_ReturnsToken()
        {
            // Arrange
            var steamId = "76561198000000000";
            var token = _store.CreateRefreshToken(steamId);

            // Act
            var result = _store.GetRefreshToken(token);

            // Assert
            result.Should().NotBeNull();
            result!.SteamId.Should().Be(steamId);
            result.Token.Should().Be(token);
        }

        [Fact]
        public void GetRefreshToken_WithInvalidToken_ReturnsNull()
        {
            // Act
            var result = _store.GetRefreshToken("invalid-token");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void RevokeRefreshToken_RemovesToken()
        {
            // Arrange
            var steamId = "76561198000000000";
            var token = _store.CreateRefreshToken(steamId);

            // Act
            _store.RevokeRefreshToken(token);
            var result = _store.GetRefreshToken(token);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetRefreshToken_WithExpiredToken_ReturnsNull()
        {
            // Arrange
            var configValuesExpired = new Dictionary<string, string>
            {
                ["Jwt:RefreshTokenLifetimeDays"] = "-1"
            };

            var configExpired = new ConfigurationBuilder()
                .AddInMemoryCollection(configValuesExpired!)
                .Build();

            var storeExpired = new InMemoryRefreshTokenStore(configExpired);
            
            var steamId = "76561198000000000";
            var token = storeExpired.CreateRefreshToken(steamId);

            // Act
            var result = storeExpired.GetRefreshToken(token);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void CleanupExpiredTokens_RemovesExpiredOnly()
        {
            // Arrange
            var configValuesExpired = new Dictionary<string, string>
            {
                ["Jwt:RefreshTokenLifetimeDays"] = "-1"
            };

            var configExpired = new ConfigurationBuilder()
                .AddInMemoryCollection(configValuesExpired!)
                .Build();

            var storeExpired = new InMemoryRefreshTokenStore(configExpired);
            
            var expiredToken = storeExpired.CreateRefreshToken("expired-user");
            var validToken = _store.CreateRefreshToken("valid-user");

            // Act
            _store.CleanupExpiredTokens();

            // Assert
            _store.GetRefreshToken(validToken).Should().NotBeNull();
        }
    }
}
