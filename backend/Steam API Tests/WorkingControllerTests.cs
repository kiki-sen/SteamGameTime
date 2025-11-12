using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Steam_API.Controllers;
using Steam_API.Dto.Output;
using Steam_API.Services;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;

namespace Steam_API_Tests
{
    public class WorkingAuthControllerTests
    {
        private readonly IConfiguration _config;
        private readonly Mock<IRefreshTokenStore> _mockRefreshTokenStore;
        private readonly JwtTokenService _jwtService;
        private readonly SteamAuthController _controller;

        public WorkingAuthControllerTests()
        {
            var configValues = new Dictionary<string, string>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenLifetimeMinutes"] = "60",
                ["Frontend:BaseUrl"] = "http://localhost:4200"
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues!)
                .Build();

            _mockRefreshTokenStore = new Mock<IRefreshTokenStore>();

            // Create real JWT service (since it's not easily mockable)
            var key = "super-secret-key-that-is-long-enough-for-hmac-sha256-algorithm";
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            _jwtService = new JwtTokenService(signingKey, _config);
            
            _mockRefreshTokenStore.Setup(x => x.CreateRefreshToken(It.IsAny<string>())).Returns("mock-refresh-token");
            _controller = new SteamAuthController(_jwtService, _mockRefreshTokenStore.Object, _config);
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Act & Assert
            Assert.NotNull(_controller);
        }


        [Fact]
        public void Callback_WithValidSteamId_ReturnsRedirectWithToken()
        {
            // Arrange
            var steamId = "76561198000000000";
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, steamId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.Callback();

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Contains("http://localhost:4200/auth/callback", redirectResult.Url);
            Assert.Contains("token=", redirectResult.Url);
            Assert.Contains("refreshToken=", redirectResult.Url);
        }

        [Fact]
        public void Me_WithAuthenticatedUser_ReturnsProfile()
        {
            // Arrange
            var steamId = "76561198000000000";
            var claims = new List<Claim>
            {
                new Claim("steamId", steamId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.Me();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var profile = Assert.IsType<ProfileDto>(okResult.Value);
            Assert.Equal(steamId, profile.SteamId64);
        }

        [Fact]
        public void Me_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var identity = new ClaimsIdentity(); // Not authenticated
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = _controller.Me();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
    }

    public class WorkingFriendsControllerTests
    {
        private readonly Mock<IFriendsService> _mockFriendsService;
        private readonly FriendsController _controller;

        public WorkingFriendsControllerTests()
        {
            _mockFriendsService = new Mock<IFriendsService>();
            _controller = new FriendsController(_mockFriendsService.Object);
        }

        [Fact]
        public async Task Leaderboard_WithValidSteamId_ReturnsLeaderboard()
        {
            // Arrange
            var steamId = "76561198000000000";
            var claims = new List<Claim>
            {
                new Claim("steamId", steamId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedLeaderboard = new FriendsLeaderboardDto();
            _mockFriendsService.Setup(x => x.GetLeaderboardAsync(steamId, null, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(expectedLeaderboard);

            // Act
            var result = await _controller.Leaderboard();

            // Assert
            var actionResult = Assert.IsType<ActionResult<FriendsLeaderboardDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(expectedLeaderboard, okResult.Value);
        }

        [Fact]
        public async Task Leaderboard_WithoutSteamId_ReturnsUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.Leaderboard();

            // Assert
            var actionResult = Assert.IsType<ActionResult<FriendsLeaderboardDto>>(result);
            Assert.IsType<UnauthorizedResult>(actionResult.Result);
        }
    }
}