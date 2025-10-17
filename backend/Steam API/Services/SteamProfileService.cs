using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Steam_API.Dto.Output;
using Steam_API.Dto.Input;

namespace Steam_API.Services
{
    public interface ISteamProfileService
    {
        Task<ProfileDto> GetProfileAsync(string steamId64, CancellationToken ct = default);
    }

    public sealed class SteamProfileService(IMemoryCache cache, IConfiguration cfg) : ISteamProfileService
    {
        private readonly string _apiKey = cfg["Steam:ApiKey"] ?? throw new("Steam:ApiKey missing");

        public async Task<ProfileDto> GetProfileAsync(string steamId64, CancellationToken ct = default)
        {
            // cache key per-user, short TTL (avatars/names don’t change often)
            var key = $"profile:{steamId64}";
            if (cache.TryGetValue(key, out ProfileDto? cached))
            {
                if (cached != null)
                {
                    return cached;
                }
            }

            // 1) player summaries (batch-capable, but we only need 1)
            var summaries = await "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/"
                .SetQueryParams(new { key = _apiKey, steamids = steamId64 })
                .GetJsonAsync<PlayerSummariesResponse>(HttpCompletionOption.ResponseContentRead, ct);

            var player = summaries.Response?.players.FirstOrDefault();
            if (player is null)
            {
                throw new InvalidOperationException("Steam profile not found or not public.");
            }

            // 2) level
            var levelResp = await "https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/"
                .SetQueryParams(new { key = _apiKey, steamid = steamId64 })
                .GetJsonAsync<SteamLevelResponse>(HttpCompletionOption.ResponseContentRead, ct);

            var dto = new ProfileDto
            {
                SteamId64 = player.Steamid,
                PersonaName = player.Personaname ?? "",
                SteamLevel = levelResp.Response?.PlayerLevel,
                AvatarSmall = player.Avatar,
                AvatarMedium = player.Avatarmedium,
                AvatarFull = player.Avatarfull,
                CountryCode = player.Loccountrycode,
                CommunityVisibilityState = player.Communityvisibilitystate,
                PersonaState = player.Personastate,
                TimeCreatedUtc = player.Timecreated.HasValue ? DateTimeOffset.FromUnixTimeSeconds(player.Timecreated.Value) : null,
                LastLogOffUtc = player.Lastlogoff.HasValue ? DateTimeOffset.FromUnixTimeSeconds(player.Lastlogoff.Value) : null
            };

            cache.Set(key, dto, TimeSpan.FromMinutes(10));
            return dto;
        }
    }
}


