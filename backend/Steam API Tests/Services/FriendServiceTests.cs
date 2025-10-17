using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Steam_API.Services;
using Steam_API_Tests.TestHelpers;
using FluentAssertions;

namespace Steam_API_Tests.Services
{
    public class FriendServiceTests : TestBase
    {
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly FriendsService _service;

        public FriendServiceTests()
        {
            _mockCache = new Mock<IMemoryCache>();
            _service = new FriendsService(_mockCache.Object, MockConfiguration.Object);
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Act & Assert
            _service.Should().NotBeNull().And.BeAssignableTo<IFriendsService>();
        }

        [Fact]
        public void Constructor_WithNullCache_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => new FriendsService(null!, MockConfiguration.Object);
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("cache");
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => new FriendsService(_mockCache.Object, null!);
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("cfg");
        }

        [Fact]
        public void Constructor_WithMissingApiKey_ThrowsException()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);
            mockConfig.Setup(x => x.GetSection("Steam")).Returns(mockSection.Object);
            mockConfig.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);

            // Act & Assert
            var act = () => new FriendsService(_mockCache.Object, mockConfig.Object);
            act.Should().Throw<Exception>()
                .WithMessage("Steam:ApiKey missing");
        }

        // Note: Testing the actual API methods would require more complex setup
        // including mocking HttpClient/Flurl behavior, which would need additional
        // infrastructure. The HTTP calls are tested through integration tests.
    }
}