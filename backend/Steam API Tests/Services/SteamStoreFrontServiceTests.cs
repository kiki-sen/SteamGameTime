using Flurl.Http;
using Flurl.Http.Testing;
using Microsoft.Extensions.Caching.Memory;
using Steam_API.Dto.Input;
using Steam_API.Services;
using FluentAssertions;

namespace Steam_API_Tests.Services
{
    public class SteamStoreFrontServiceTests : IDisposable
    {
        private readonly HttpTest _httpTest;
        private readonly SteamStoreFrontService _service;
        private readonly IMemoryCache _cache;
        private readonly Flurl.Http.Configuration.IFlurlClientCache _clientCache;

        public SteamStoreFrontServiceTests()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            FlurlHttp.Clients.Remove("steam-store");
            _httpTest = new HttpTest();
            _clientCache = FlurlHttp.Clients;
            _clientCache.GetOrAdd("steam-store", "https://store.steampowered.com");
            _service = new SteamStoreFrontService(_clientCache, _cache);
        }

        [Fact]
        public async Task GetPlatformsAsync_WithValidAppId_ReturnsPlatformInfo()
        {
            // Arrange
            var appId = 570; // Dota 2
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>
            {
                ["570"] = new StoreAppDetailsWrapper
                {
                    Success = true,
                    Data = new StoreAppDetails
                    {
                        Platforms = new Steam_API.Dto.Input.PlatformsDto
                        {
                            windows = true,
                            mac = true,
                            linux = true
                        }
                    }
                }
            };

            _httpTest.RespondWithJson(storeResponse);

            // Act
            var result = await _service.GetPlatformsAsync(appId);

            // Assert
            result.Should().NotBeNull();
            result.appId.Should().Be(appId);
            result.windows.Should().BeTrue();
            result.mac.Should().BeTrue();
            result.linux.Should().BeTrue();

            // Verify the API was called with correct parameters
            _httpTest.ShouldHaveCalled("https://store.steampowered.com/api/appdetails*")
                    .WithQueryParam("appids", "570")
                    .WithQueryParam("filters", "platforms")
                    .Times(1);
        }

        [Fact]
        public async Task GetPlatformsAsync_WindowsOnly_ReturnsCorrectPlatforms()
        {
            // Arrange
            var appId = 440; // Team Fortress 2
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>
            {
                ["440"] = new StoreAppDetailsWrapper
                {
                    Success = true,
                    Data = new StoreAppDetails
                    {
                        Platforms = new Steam_API.Dto.Input.PlatformsDto
                        {
                            windows = true,
                            mac = false,
                            linux = false
                        }
                    }
                }
            };

            _httpTest.RespondWithJson(storeResponse);

            // Act
            var result = await _service.GetPlatformsAsync(appId);

            // Assert
            result.Should().NotBeNull();
            result.appId.Should().Be(appId);
            result.windows.Should().BeTrue();
            result.mac.Should().BeFalse();
            result.linux.Should().BeFalse();
        }

        [Fact]
        public async Task GetPlatformsAsync_WithInvalidAppId_ReturnsNull()
        {
            // Arrange
            var appId = 999999;
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>
            {
                ["999999"] = new StoreAppDetailsWrapper
                {
                    Success = false,
                    Data = null
                }
            };

            _httpTest.RespondWithJson(storeResponse);

            // Act
            var result = await _service.GetPlatformsAsync(appId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPlatformsAsync_CachesResults()
        {
            // Arrange
            var appId = 730; // CS:GO
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>
            {
                ["730"] = new StoreAppDetailsWrapper
                {
                    Success = true,
                    Data = new StoreAppDetails
                    {
                        Platforms = new Steam_API.Dto.Input.PlatformsDto
                        {
                            windows = true,
                            mac = true,
                            linux = true
                        }
                    }
                }
            };

            _httpTest.RespondWithJson(storeResponse);

            // Act - Call twice
            var result1 = await _service.GetPlatformsAsync(appId);
            var result2 = await _service.GetPlatformsAsync(appId);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1.appId.Should().Be(result2.appId);

            // Should only call API once due to caching
            _httpTest.ShouldHaveCalled("https://store.steampowered.com/api/appdetails*")
                    .Times(1);
        }

        [Fact]
        public async Task GetAppDetails_WithValidAppId_ReturnsAppDetails()
        {
            // Arrange
            var appId = 570;
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>
            {
                ["570"] = new StoreAppDetailsWrapper
                {
                    Success = true,
                    Data = new StoreAppDetails
                    {
                        Platforms = new Steam_API.Dto.Input.PlatformsDto
                        {
                            windows = true,
                            mac = true,
                            linux = true
                        }
                    }
                }
            };

            _httpTest.RespondWithJson(storeResponse);

            // Act
            var result = await _service.GetAppDetails(appId);

            // Assert
            result.Should().NotBeNull();
            result.Platforms.Should().NotBeNull();
            result.Platforms.linux.Should().BeTrue();

            // Verify API call without filters parameter
            _httpTest.ShouldHaveCalled("https://store.steampowered.com/api/appdetails*")
                    .WithQueryParam("appids", "570")
                    .WithoutQueryParam("filters")
                    .Times(1);
        }

        [Fact]
        public async Task GetAppDetails_WithInvalidAppId_ReturnsNull()
        {
            // Arrange
            var appId = 999999;
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>
            {
                ["999999"] = new StoreAppDetailsWrapper
                {
                    Success = false,
                    Data = null
                }
            };

            _httpTest.RespondWithJson(storeResponse);

            // Act
            var result = await _service.GetAppDetails(appId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAppDetails_CachesResults()
        {
            // Arrange
            var appId = 440;
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>
            {
                ["440"] = new StoreAppDetailsWrapper
                {
                    Success = true,
                    Data = new StoreAppDetails
                    {
                        Platforms = new Steam_API.Dto.Input.PlatformsDto
                        {
                            windows = true,
                            mac = false,
                            linux = false
                        }
                    }
                }
            };

            _httpTest.RespondWithJson(storeResponse);

            // Act - Call twice
            var result1 = await _service.GetAppDetails(appId);
            var result2 = await _service.GetAppDetails(appId);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();

            // Should only call API once due to caching
            _httpTest.ShouldHaveCalled("https://store.steampowered.com/api/appdetails*")
                    .Times(1);
        }

        [Fact]
        public async Task GetPlatformsAsync_WithMissingAppIdInResponse_ReturnsNull()
        {
            // Arrange
            var appId = 123;
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>();

            _httpTest.RespondWithJson(storeResponse);

            // Act
            var result = await _service.GetPlatformsAsync(appId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPlatformsAsync_WithNullPlatforms_ReturnsNull()
        {
            // Arrange
            var appId = 456;
            var storeResponse = new Dictionary<string, StoreAppDetailsWrapper>
            {
                ["456"] = new StoreAppDetailsWrapper
                {
                    Success = true,
                    Data = new StoreAppDetails
                    {
                        Platforms = null
                    }
                }
            };

            _httpTest.RespondWithJson(storeResponse);

            // Act
            var result = await _service.GetPlatformsAsync(appId);

            // Assert
            result.Should().BeNull();
        }

        public void Dispose()
        {
            _httpTest?.Dispose();
            _cache?.Dispose();
        }
    }
}
