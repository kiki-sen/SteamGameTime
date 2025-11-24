using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Steam_API.Dto.Input;

namespace Steam_API.Services
{
    public interface ISteamStoreFrontService
    {
        Task<StoreAppDetails?> GetAppDetails(int appId, CancellationToken ct = default);
        Task<Dto.Output.PlatformsDto?> GetPlatformsAsync(int appId, CancellationToken ct = default);
    }

    public class SteamStoreFrontService(IFlurlClientCache clientCache, IMemoryCache memoryCache) : ISteamStoreFrontService
    {
        private readonly IFlurlClient _client = clientCache.Get("steam-store");
        private readonly SemaphoreSlim _rateGate = new(1, 1);
        private DateTime _nextAllowedUtc = DateTime.MinValue;

        private async Task EnforceRateLimitAsync(CancellationToken ct)
        {
            await _rateGate.WaitAsync(ct);
            try
            {
                var now = DateTime.UtcNow;
                if (now < _nextAllowedUtc)
                {
                    var delay = _nextAllowedUtc - now;
                    await Task.Delay(delay, ct);
                }

                // allow next call after 400ms (≈ 2.5 req/s). Tweak as needed.
                _nextAllowedUtc = DateTime.UtcNow.AddMilliseconds(150);
            }
            finally
            {
                _rateGate.Release();
            }
        }

        public async Task<StoreAppDetails?> GetAppDetails(int appId, CancellationToken ct = default)
        {
            return await memoryCache.GetOrCreateAsync($"app:{appId}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30);

                await EnforceRateLimitAsync(ct);

                var url = _client
                    .Request("api", "appdetails")
                    .SetQueryParam("appids", appId);

                var json = await url.GetJsonAsync<Dictionary<string, StoreAppDetailsWrapper>>(HttpCompletionOption.ResponseContentRead, ct);
                if (!json.TryGetValue(appId.ToString(), out var root))
                {
                    return null;
                }

                return root.Data;
            });
        }

        public async Task<Dto.Output.PlatformsDto?> GetPlatformsAsync(int appId, CancellationToken ct = default)
        {
            return await memoryCache.GetOrCreateAsync($"plat:{appId}", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30);

                await EnforceRateLimitAsync(ct);

                var url = _client
                    .Request("api", "appdetails")
                    .SetQueryParam("appids", appId)
                    .SetQueryParam("filters", "platforms"); // trims payload

                var json = await url.GetJsonAsync<Dictionary<string, StoreAppDetailsWrapper>>(HttpCompletionOption.ResponseContentRead, ct);
                if (!json.TryGetValue(appId.ToString(), out var root))
                {
                    return null;
                }

                var platforms = root.Data?.Platforms;
                if (platforms == null || !root.Success)
                {
                    return null;
                }

                return new Dto.Output.PlatformsDto
                {
                    appId = appId,
                    linux = platforms.linux,
                    windows = platforms.windows,
                    mac = platforms.mac
                };
            });
        }
    }
}
