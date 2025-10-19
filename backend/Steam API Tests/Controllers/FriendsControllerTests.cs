using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Steam_API.Controllers;
using Steam_API.Dto.Output;
using Steam_API.Services;
using Steam_API_Tests.TestHelpers;
using System.Security.Claims;
using FluentAssertions;

namespace Steam_API_Tests.Controllers
{
    public class FriendsControllerTests : TestBase
    {
        private readonly Mock<IFriendsService> _mockFriendsService;
        private readonly FriendsController _controller;

        public FriendsControllerTests()
        {
            _mockFriendsService = new Mock<IFriendsService>();
            _controller = new FriendsController(_mockFriendsService.Object);
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Act & Assert
            _controller.Should().NotBeNull();
        }

        [Fact]
        public async Task Leaderboard_WithValidSteamId_ReturnsLeaderboard()
        {
            // Arrange
            var steamId = "76561198000000000";
            var claims = new List<Claim>
            {
                new("steamId", steamId)
            };

            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(x => x.User)
                .Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedLeaderboard = new FriendsLeaderboardDto();
            _mockFriendsService
                .Setup(x => x.GetLeaderboardAsync(steamId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLeaderboard);

            // Act
            var result = await _controller.Leaderboard();

            // Assert
            result.Should().BeOfType<ActionResult<FriendsLeaderboardDto>>()
                .Which.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(expectedLeaderboard);
        }

        [Fact]
        public async Task Leaderboard_WithAppId_CallsServiceWithAppId()
        {
            // Arrange
            var steamId = "76561198000000000";
            var appId = 12345;
            var claims = new List<Claim>
            {
                new("steamId", steamId)
            };

            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(x => x.User)
                .Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedLeaderboard = new FriendsLeaderboardDto();
            _mockFriendsService
                .Setup(x => x.GetLeaderboardAsync(steamId, appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLeaderboard);

            // Act
            var result = await _controller.Leaderboard(appId);

            // Assert
            _mockFriendsService.Verify(x => x.GetLeaderboardAsync(steamId, appId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Leaderboard_WithoutSteamId_ReturnsUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(x => x.User)
                .Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.Leaderboard();

            // Assert
            result.Should().BeOfType<ActionResult<FriendsLeaderboardDto>>()
                .Which.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task Leaderboard_WithNameIdentifierClaim_Works()
        {
            // Arrange
            var steamId = "76561198000000000";
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, steamId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(x => x.User)
                .Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedLeaderboard = new FriendsLeaderboardDto();
            _mockFriendsService
                .Setup(x => x.GetLeaderboardAsync(steamId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLeaderboard);

            // Act
            var result = await _controller.Leaderboard();

            // Assert
            result.Should().BeOfType<ActionResult<FriendsLeaderboardDto>>()
                .Which.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(expectedLeaderboard);
        }

        [Fact]
        public async Task List_WithValidSteamId_ReturnsFriendsList()
        {
            // Arrange
            var steamId = "76561198000000000";
            var claims = new List<Claim>
            {
                new("steamId", steamId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(x => x.User)
                .Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedFriendsList = new FriendsListDto();
            _mockFriendsService
                .Setup(x => x.GetFriendsListAsync(steamId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFriendsList);

            // Act
            var result = await _controller.List();

            // Assert
            result.Should().BeOfType<ActionResult<FriendsListDto>>()
                .Which.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(expectedFriendsList);
        }

        [Fact]
        public async Task List_WithIncludeSelfFalse_CallsServiceCorrectly()
        {
            // Arrange
            var steamId = "76561198000000000";
            var claims = new List<Claim>
            {
                new("steamId", steamId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(x => x.User)
                .Returns(principal);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedFriendsList = new FriendsListDto();
            _mockFriendsService
                .Setup(x => x.GetFriendsListAsync(steamId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFriendsList);

            // Act
            var result = await _controller.List(includeSelf: false);

            // Assert
            _mockFriendsService.Verify(x => x.GetFriendsListAsync(steamId, false, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}