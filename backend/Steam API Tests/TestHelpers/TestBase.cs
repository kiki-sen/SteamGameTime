using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Steam_API_Tests.TestHelpers
{
    public abstract class TestBase
    {
        protected Mock<IConfiguration> MockConfiguration { get; }
        protected Mock<ILogger<T>> CreateMockLogger<T>() => new();

        protected TestBase()
        {
            MockConfiguration = new Mock<IConfiguration>();
            SetupDefaultConfiguration();
        }

        private void SetupDefaultConfiguration()
        {
            var mockSteamSection = new Mock<IConfigurationSection>();
            mockSteamSection.Setup(x => x["ApiKey"]).Returns("test-api-key");
            MockConfiguration.Setup(x => x.GetSection("Steam")).Returns(mockSteamSection.Object);
            MockConfiguration.Setup(x => x["Steam:ApiKey"]).Returns("test-api-key");
            MockConfiguration.Setup(x => x["Steam:WebApiKey"]).Returns("test-web-api-key");

            var mockJwtSection = new Mock<IConfigurationSection>();
            mockJwtSection.Setup(x => x["Issuer"]).Returns("test-issuer");
            mockJwtSection.Setup(x => x["Audience"]).Returns("test-audience");
            MockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(mockJwtSection.Object);

            MockConfiguration.Setup(x => x["Frontend:BaseUrl"]).Returns("http://localhost:4200");
        }

        protected IConfigurationSection CreateMockConfigurationSection(string key, string value)
        {
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x[key]).Returns(value);
            return mockSection.Object;
        }
    }
}