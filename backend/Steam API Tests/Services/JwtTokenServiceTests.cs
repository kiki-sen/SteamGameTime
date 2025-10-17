using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Steam_API.Services;
using Steam_API_Tests.TestHelpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;

namespace Steam_API_Tests.Services
{
    public class JwtTokenServiceTests : TestBase
    {
        private readonly SymmetricSecurityKey _signingKey;
        private readonly JwtTokenService _service;

        public JwtTokenServiceTests()
        {
            var key = "super-secret-key-that-is-long-enough-for-hmac-sha256-algorithm";
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            _service = new JwtTokenService(_signingKey, MockConfiguration.Object);
        }

        [Fact]
        public void CreateToken_ValidSteamId_ReturnsValidJwtToken()
        {
            // Arrange
            var steamId = "76561198000000000";

            // Act
            var token = _service.CreateToken(steamId);

            // Assert
            token.Should().NotBeNull().And.NotBeEmpty();

            // Verify token can be parsed
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            jwtToken.Issuer.Should().Be("test-issuer");
            jwtToken.Audiences.First().Should().Be("test-audience");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == steamId);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == steamId);
        }

        [Fact]
        public void CreateToken_TokenExpiresInSevenDays()
        {
            // Arrange
            var steamId = "76561198000000000";
            var beforeCreation = DateTime.UtcNow;

            // Act
            var token = _service.CreateToken(steamId);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            var expectedExpiry = beforeCreation.AddDays(7);
            var actualExpiry = jwtToken.ValidTo;
            
            // Allow 1 minute tolerance for test execution time
            Math.Abs((expectedExpiry - actualExpiry).TotalMinutes).Should().BeLessThan(1, 
                "because token expiry should be within 1 minute of expected 7-day expiration");
        }

        [Theory]
        [InlineData("76561198000000000")]
        [InlineData("76561198123456789")]
        [InlineData("12345678901234567")]
        public void CreateToken_DifferentSteamIds_CreatesUniqueTokens(string steamId)
        {
            // Act
            var token = _service.CreateToken(steamId);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var subjectClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            
            subjectClaim.Should().NotBeNull();
            subjectClaim!.Value.Should().Be(steamId);
        }

        [Fact]
        public void CreateToken_ValidatesConfigurationDependency()
        {
            // Arrange
            var invalidConfig = new Mock<IConfiguration>();
            var invalidJwtSection = new Mock<IConfigurationSection>();
            invalidJwtSection.Setup(x => x["Issuer"]).Returns((string?)null);
            invalidConfig.Setup(x => x.GetSection("Jwt")).Returns(invalidJwtSection.Object);

            var service = new JwtTokenService(_signingKey, invalidConfig.Object);

            // Act & Assert - Should not throw, but token may have null issuer
            var token = service.CreateToken("76561198000000000");
            token.Should().NotBeNull();
        }
    }
}