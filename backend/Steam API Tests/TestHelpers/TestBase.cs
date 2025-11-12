using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace Steam_API_Tests.TestHelpers
{
    public abstract class TestBase
    {
        protected IConfiguration Configuration { get; }
        protected Mock<ILogger<T>> CreateMockLogger<T>() => new();

        protected TestBase()
        {
            var configValues = new Dictionary<string, string>
            {
                ["Steam:ApiKey"] = "test-api-key",
                ["Steam:WebApiKey"] = "test-web-api-key",
                ["Jwt:Secret"] = "super-secret-key-that-is-at-least-32-characters-long-for-HMAC-SHA256",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenLifetimeMinutes"] = "60",
                ["Jwt:RefreshTokenLifetimeDays"] = "30",
                ["Frontend:BaseUrl"] = "http://localhost:4200"
            };

            Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues!)
                .Build();
        }

        protected IConfigurationSection CreateMockConfigurationSection(string key, string value)
        {
            var mockSection = new Mock<IConfigurationSection>();
            mockSection
                .Setup(x => x[key])
                .Returns(value);

            return mockSection.Object;
        }
    }
}