using Microsoft.Extensions.Configuration;
using Moq;
using Steam_API.Services;
using Steam_API_Tests.TestHelpers;
using FluentAssertions;

namespace Steam_API_Tests.Services
{
    public class SteamApiClientTests : TestBase
    {
        [Fact]
        public void Constructor_WithValidConfiguration_CreatesInstance()
        {
            // Act
            var client = new SteamApiClient(MockConfiguration.Object);

            // Assert
            client.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithMissingApiKey_ThrowsException()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();
            mockSection
                .Setup(x => x[It.IsAny<string>()])
                .Returns((string?)null);

            mockConfig
                .Setup(x => x.GetSection("Steam"))
                .Returns(mockSection.Object);

            mockConfig
                .Setup(x => x[It.IsAny<string>()])
                .Returns((string?)null);

            // Act & Assert
            var act = () => new SteamApiClient(mockConfig.Object);
            act.Should().Throw<Exception>()
                .WithMessage("Steam:ApiKey missing");
        }
    }
}