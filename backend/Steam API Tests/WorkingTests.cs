using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Steam_API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Steam_API_Tests
{
    public class WorkingJwtTokenServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SymmetricSecurityKey _signingKey;

        public WorkingJwtTokenServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            
            var mockJwtSection = new Mock<IConfigurationSection>();
            mockJwtSection.Setup(x => x["Issuer"]).Returns("test-issuer");
            mockJwtSection.Setup(x => x["Audience"]).Returns("test-audience");
            _mockConfig.Setup(x => x.GetSection("Jwt")).Returns(mockJwtSection.Object);
            
            var key = "super-secret-key-that-is-long-enough-for-hmac-sha256-algorithm";
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }

        [Fact]
        public void CreateToken_ValidSteamId_ReturnsValidJwtToken()
        {
            // Arrange
            var service = new JwtTokenService(_signingKey, _mockConfig.Object);
            var steamId = "76561198000000000";

            // Act
            var token = service.CreateToken(steamId);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            // Verify token can be parsed
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            Assert.Equal("test-issuer", jwtToken.Issuer);
            Assert.Equal("test-audience", jwtToken.Audiences.First());
            Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == steamId);
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == steamId);
        }

        [Fact]
        public void CreateToken_TokenExpiresInSevenDays()
        {
            // Arrange
            var service = new JwtTokenService(_signingKey, _mockConfig.Object);
            var steamId = "76561198000000000";
            var beforeCreation = DateTime.UtcNow;

            // Act
            var token = service.CreateToken(steamId);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            var expectedExpiry = beforeCreation.AddDays(7);
            var actualExpiry = jwtToken.ValidTo;
            
            // Allow 1 minute tolerance for test execution time
            Assert.True(Math.Abs((expectedExpiry - actualExpiry).TotalMinutes) < 1);
        }

        [Theory]
        [InlineData("76561198000000000")]
        [InlineData("76561198123456789")]
        [InlineData("12345678901234567")]
        public void CreateToken_DifferentSteamIds_CreatesUniqueTokens(string steamId)
        {
            // Arrange
            var service = new JwtTokenService(_signingKey, _mockConfig.Object);

            // Act
            var token = service.CreateToken(steamId);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var subjectClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            
            Assert.NotNull(subjectClaim);
            Assert.Equal(steamId, subjectClaim.Value);
        }
    }

    public class WorkingFriendsServiceTests
    {
        [Fact]
        public void FriendsService_ImplementsInterface()
        {
            // This test verifies the service implements the correct interface
            var mockCache = new Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var mockConfig = new Mock<IConfiguration>();
            
            // Setup mock to return a valid API key
            mockConfig.Setup(x => x["Steam:ApiKey"]).Returns("test-api-key");

            // Act & Assert - should not throw
            var service = new Steam_API.Services.FriendsService(mockCache.Object, mockConfig.Object);
            Assert.True(service is Steam_API.Services.IFriendsService);
        }
    }

    public class WorkingSteamApiClientTests
    {
        [Fact]
        public void SteamApiClient_CanBeCreated_WithValidConfig()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(x => x["Steam:ApiKey"]).Returns("test-api-key");

            // Act & Assert - should not throw
            var client = new Steam_API.Services.SteamApiClient(mockConfig.Object);
            Assert.NotNull(client);
        }
    }
}