using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Steam_API.Dto.Input;
using Steam_API.Dto.Output;
using System.Collections.Concurrent;

namespace Steam_API.Services
{
    public interface IFriendsService
    {
        Task<FriendsLeaderboardDto> GetLeaderboardAsync(
            string meSteamId,
            int? appId = null,
            CancellationToken ct = default);

        Task<FriendsListDto> GetFriendsListAsync(
            string meSteamId, 
            bool includeSelf = true,
            CancellationToken ct = default);
    }

    public sealed class FriendsService(IMemoryCache cache, IConfiguration cfg) : IFriendsService
    {
        // throttle outbound calls (be nice to Steam)
        private static readonly SemaphoreSlim Gate = new(8); // 8 concurrent friend calls
        private static readonly SemaphoreSlim LevelGate = new(8); // throttle parallel level calls
        private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        private readonly string _apiKey = (cfg ?? throw new ArgumentNullException(nameof(cfg)))["Steam:ApiKey"] ?? throw new Exception("Steam:ApiKey missing");

        public async Task<FriendsLeaderboardDto> GetLeaderboardAsync(
            string meSteamId, int? appId = null, CancellationToken ct = default)
        {
            // 1) Friends list
            var friends = await "https://api.steampowered.com/ISteamUser/GetFriendList/v1/"
                .SetQueryParams(new { key = _apiKey, steamid = meSteamId, relationship = "friend" })
                .GetJsonAsync<FriendListResponse>(HttpCompletionOption.ResponseContentRead, ct); 

            var ids = friends.Friendslist?.Friends?.Select(f => f.Steamid).Distinct().ToList() ?? [];

            // Include self at the end for convenience
            if (!ids.Contains(meSteamId)) ids.Add(meSteamId);

            // 2) Summaries (batch up to 100 ids per call)
            var summaries = await GetSummariesAsync(ids, ct);
            var byId = summaries.ToDictionary(s => s.Steamid);

            // Filter to profiles we can show a name/avatar for
            var publicish = ids.Where(id => byId.ContainsKey(id)).ToList();

            // 3) For each friend, fetch hours
            var rows = new ConcurrentBag<FriendHoursRow>();
            var tasks = publicish.Select(async id =>
            {
                await Gate.WaitAsync(ct);
                try
                {
                    var s = byId[id];
                    var (hours, hours2w, ok) = appId.HasValue
                        ? await GetHoursForAppAsync(id, appId.Value, ct)
                        : await GetTotalHoursAsync(id, ct);

                    rows.Add(new FriendHoursRow
                    {
                        SteamId64 = id,
                        PersonaName = s.Personaname ?? "(unknown)",
                        AvatarMedium = s.Avatarmedium ?? "",
                        IsYou = id == meSteamId,
                        HoursTotal = hours,
                        Hours2Weeks = appId.HasValue ? hours2w : null,
                        PrivateOrUnavailable = !ok
                    });
                }
                finally { Gate.Release(); }
            });

            await Task.WhenAll(tasks);

            var ordered = rows
                .OrderByDescending(r => r.HoursTotal)
                .ThenBy(r => r.PersonaName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new FriendsLeaderboardDto { AppId = appId, Rows = ordered };
        }

        // --- helpers ---

        private async Task<List<Player>> GetSummariesAsync(List<string> ids, CancellationToken ct)
        {
            var all = new List<Player>();
            foreach (var chunk in ids.Chunk(100))
            {
                var flat = string.Join(',', chunk);
                var resp = await "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/"
                    .SetQueryParams(new { key = _apiKey, steamids = flat })
                    .GetJsonAsync<PlayerSummariesResponse>(HttpCompletionOption.ResponseContentRead, ct);

                if (resp.Response?.players != null) all.AddRange(resp.Response.players);
            }
            return all;
        }

        // App-specific hours (fast via appids_filter)
        private async Task<(double hours, double? hours2w, bool ok)> GetHoursForAppAsync(
            string steamId, int appId, CancellationToken ct)
        {
            var cacheKey = $"owned:{steamId}:app:{appId}";
            if (_cache.TryGetValue(cacheKey, out (double, double?, bool) cached)) return cached;

            try
            {
                // Owned games filtered to appId
                var owned = await "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/"
                    .SetQueryParams(new
                    {
                        key = _apiKey,
                        steamid = steamId,
                        include_appinfo = 0
                    })
                    .SetQueryParam("appids_filter[0]", appId)
                    .GetJsonAsync<OwnedGamesResponse>(HttpCompletionOption.ResponseContentRead, ct);

                var minsForever = owned.response?.games?.FirstOrDefault()?.playtime_forever ?? 0;

                // Recently played (for 2 weeks minutes)
                var recent = await "https://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v1/"
                    .SetQueryParams(new { key = _apiKey, steamid = steamId })
                    .GetJsonAsync<RecentlyPlayedResponse>(HttpCompletionOption.ResponseContentRead, ct);

                var mins2w = recent.Response?.Games?.FirstOrDefault(g => g.AppId == appId)?.Playtime2Weeks ?? 0;

                var result = (minsForever / 60.0, mins2w / 60.0, ok: true);
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
                return result;
            }
            catch
            {
                var result = (0.0, (double?)null, ok: false);
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                return result;
            }
        }

        // Total library hours (heavier)
        private async Task<(double hours, double? hours2w, bool ok)> GetTotalHoursAsync(
            string steamId, CancellationToken ct)
        {
            var cacheKey = $"owned:{steamId}:all";
            if (_cache.TryGetValue(cacheKey, out (double, double?, bool) cached)) return cached;

            try
            {
                var owned = await "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/"
                    .SetQueryParams(new { key = _apiKey, steamid = steamId, include_appinfo = 0 })
                    .GetJsonAsync<OwnedGamesResponse>(HttpCompletionOption.ResponseContentRead, ct);

                var minsForever = owned.response?.games?.Sum(g => g.playtime_forever) ?? 0;

                var recent = await "https://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v1/"
                    .SetQueryParams(new { key = _apiKey, steamid = steamId })
                    .GetJsonAsync<RecentlyPlayedResponse>(HttpCompletionOption.ResponseContentRead, ct);

                var mins2w = recent.Response?.Games?.Sum(g => g.Playtime2Weeks ?? 0) ?? 0;

                var result = (minsForever / 60.0, mins2w / 60.0, ok: true);
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
                return result;
            }
            catch
            {
                var result = (0.0, (double?)null, ok: false);
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                return result;
            }
        }

        public async Task<FriendsListDto> GetFriendsListAsync(string meSteamId, bool includeSelf = true, CancellationToken ct = default)
        {
            // 1) Pull friend edges (ids + since)
            var friendsResp = await "https://api.steampowered.com/ISteamUser/GetFriendList/v1/"
                .SetQueryParams(new { key = _apiKey, steamid = meSteamId, relationship = "friend" })
                .GetJsonAsync<FriendListResponse>(HttpCompletionOption.ResponseContentRead, ct);

            var edges = friendsResp.Friendslist?.Friends ?? [];

            // 2) Compose list of IDs (optionally include self so the card can count you)
            var ids = edges.Select(f => f.Steamid).Distinct().ToList();
            if (includeSelf && !ids.Contains(meSteamId)) ids.Add(meSteamId);

            // 3) Batch fetch summaries (100 per call)
            var summaries = await GetSummariesAsync(ids, ct);
            var byId = summaries.ToDictionary(s => s.Steamid);

            // 4) Merge to DTO
            var rows = new List<FriendSummaryDto>(ids.Count);
            foreach (var id in ids)
            {
                if (!byId.TryGetValue(id, out var ps)) continue;

                var sinceUnix = edges.FirstOrDefault(e => e.Steamid == id)?.FriendSince;
                rows.Add(new FriendSummaryDto
                {
                    SteamId64 = id,
                    PersonaName = ps.Personaname ?? "",
                    AvatarSmall = ps.Avatar,
                    AvatarMedium = ps.Avatarmedium,
                    AvatarFull = ps.Avatarfull,
                    CommunityVisibilityState = ps.Communityvisibilitystate,
                    PersonaState = ps.Personastate,
                    FriendSinceUtc = sinceUnix.HasValue ? DateTimeOffset.FromUnixTimeSeconds(sinceUnix.Value) : null,
                    IsYou = id == meSteamId,
                    GameId = ps.Gameid,
                    GameName = ps.Gameextrainfo
                });
            }

            await Task.WhenAll(rows.Select(async r =>
            {
                r.SteamLevel = await GetLevelAsync(r.SteamId64, ct);
            }));

            return new FriendsListDto
            {
                Rows = rows
                    .OrderByDescending(r => r.IsYou)
                    .ThenBy(r => r.PersonaName, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        private async Task<int?> GetLevelAsync(string steamId, CancellationToken ct)
        {
            var key = $"steam:level:{steamId}";
            if (_cache.TryGetValue(key, out int? cached))
            {
                return cached;
            }

            await LevelGate.WaitAsync(ct);
            try
            {
                var resp = await "https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/"
                    .SetQueryParams(new { key = _apiKey, steamid = steamId })
                    .GetJsonAsync<SteamLevelResponse>(HttpCompletionOption.ResponseContentRead, ct);

                var level = resp.Response?.PlayerLevel;
                _cache.Set(key, level, TimeSpan.FromMinutes(30)); // levels don't change often
                return level;
            }
            catch
            {
                _cache.Set<string?>(key, null, TimeSpan.FromMinutes(5));
                return null;
            }
            finally 
            { 
                LevelGate.Release(); 
            }
        }
    }
}