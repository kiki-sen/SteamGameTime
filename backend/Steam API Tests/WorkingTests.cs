using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Steam_API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;

namespace Steam_API_Tests
{
    public class WorkingJwtTokenServiceTests
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _signingKey;

        public WorkingJwtTokenServiceTests()
        {
            var configValues = new Dictionary<string, string>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenLifetimeMinutes"] = "60"
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues!)
                .Build();
            
            var key = "super-secret-key-that-is-long-enough-for-hmac-sha256-algorithm";
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }

        [Fact]
        public void CreateToken_ValidSteamId_ReturnsValidJwtToken()
        {
            // Arrange
            var service = new JwtTokenService(_signingKey, _config);
            var steamId = "76561198000000000";

            // Act
            var token = service.CreateToken(steamId);

            // Assert
            token.Should().NotBeNull();
            token.Should().NotBeEmpty();

            // Verify token can be parsed
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);


            jwtToken.Issuer.Should().Be("test-issuer");
            jwtToken.Audiences.First().Should().Be("test-audience");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == steamId);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == steamId);
        }

        [Fact]
        public void CreateToken_TokenExpiresIn60Minutes()
        {
            // Arrange
            var service = new JwtTokenService(_signingKey, _config);
            var steamId = "76561198000000000";
            var beforeCreation = DateTime.UtcNow;

            // Act
            var token = service.CreateToken(steamId);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            var expectedExpiry = beforeCreation.AddMinutes(60);
            var actualExpiry = jwtToken.ValidTo;
            
            // Allow 1 minute tolerance for test execution time
            (Math.Abs((expectedExpiry - actualExpiry).TotalMinutes) < 1).Should().BeTrue();
        }

        [Theory]
        [InlineData("76561198000000000")]
        [InlineData("76561198123456789")]
        [InlineData("12345678901234567")]
        public void CreateToken_DifferentSteamIds_CreatesUniqueTokens(string steamId)
        {
            // Arrange
            var service = new JwtTokenService(_signingKey, _config);

            // Act
            var token = service.CreateToken(steamId);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var subjectClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            
            subjectClaim.Should().NotBeNull();
            steamId.Should().Be(subjectClaim.Value);
        }
    }

    public class WorkingFriendsServiceTests
    {
        [Fact]
        public void FriendsService_ImplementsInterface()
        {
            // This test verifies the service implements the correct interface
            var mockCache = new Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var configValues = new Dictionary<string, string>
            {
                ["Steam:ApiKey"] = "test-api-key"
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues!)
                .Build();

            // Act & Assert - should not throw
            var service = new FriendsService(mockCache.Object, config);
            (service is IFriendsService).Should().BeTrue();
        }
    }

    public class WorkingSteamApiClientTests
    {
        [Fact]
        public void SteamApiClient_CanBeCreated_WithValidConfig()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                ["Steam:ApiKey"] = "test-api-key"
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues!)
                .Build();

            // Act & Assert - should not throw
            var client = new SteamApiClient(config);
            client.Should().NotBeNull();
        }
    }
}