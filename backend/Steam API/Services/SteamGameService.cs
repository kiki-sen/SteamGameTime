using Flurl;
using Flurl.Http;
using Ganss.Xss;
using Steam_API.Dto.Output;
using Steam_API.Dto.Input;
using System.Net;
using System.Text.Json;

namespace Steam_API.Services
{
    public interface ISteamGameService
    {
        Task<GameDetailsDto> GetGameDetailsAsync(string steamId64, int appId, bool includeGlobal = false, string lang = "english");
    }

    public sealed class SteamGameService(IConfiguration cfg, HtmlSanitizer sanitizer) : ISteamGameService
    {
        private readonly string _apiKey = cfg["Steam:ApiKey"] ?? throw new("Steam:ApiKey missing");

        public string? Sanitize(string? html) => string.IsNullOrWhiteSpace(html) ? null : sanitizer.Sanitize(html);

        public async Task<GameDetailsDto> GetGameDetailsAsync(string steamId64, int appId, bool includeGlobal = false, string lang = "english")
        {
            // 1) Schema (achievement definitions + icons)
            var schemaTask = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/"
                .SetQueryParams(new { key = _apiKey, appid = appId, l = lang })
                .GetJsonAsync<SchemaForGameResponse>();

            // 2) User achievements (per-app)
            var userAchTask = TryGetPlayerAchievementsAsync(steamId64, appId, lang);

            // 3) Store appdetails (header image + name)
            var storeTask = "https://store.steampowered.com/api/appdetails"
                .SetQueryParams(new { appids = appId })
                .GetJsonAsync<Dictionary<string, StoreAppDetailsWrapper>>();

            // 4) Optional global achievements %
            var hasAchievements = (schemaTask.Result.Game?.AvailableGameStats?.Achievements?.Count ?? 0) > 0;
            Task<GlobalPercentResponse?> globalTask = (includeGlobal && hasAchievements)
                ? GetGlobalAsync(appId)                        
                : Task.FromResult<GlobalPercentResponse?>(null);

            // 5) CCU (current players)
            Task<CurrentPlayersResponse?> ccuTask =
                "https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/"
                    .SetQueryParams(new { appid = appId })
                    .AllowAnyHttpStatus()
                    .GetAsync()
                    .ContinueWith(async t =>
                    {
                        var resp = t.Result;
                        if (resp.ResponseMessage.IsSuccessStatusCode)
                            return await resp.GetJsonAsync<CurrentPlayersResponse>();
                        // treat 4xx/5xx as "no data"
                        return null;
                    }).Unwrap();

            await Task.WhenAll(schemaTask, userAchTask, storeTask, globalTask);

            var schema = schemaTask.Result.Game?.AvailableGameStats?.Achievements ?? [];
            var userAch = userAchTask?.Result?.PlayerStats?.Achievements ?? [];
            var store = storeTask.Result.TryGetValue(appId.ToString(), out var wrap) && wrap.Success ? wrap.Data : null;
            var global = globalTask.Result?.Achievementpercentages?.Achievements?
                         .ToDictionary(a => a.Name, a => (double?)a.Percent)
                         ?? [];
            var ccu = ccuTask.Result?.Response?.PlayerCount;

            // Build map of user states (apiname -> achieved + unlocktime)
            var userMap = userAch.ToDictionary(a => a.ApiName, a => (a.Achieved == 1, a.UnlockTimeUnix));

            // Merge schema (icons/labels) + user state (+ optional global %)
            var achievements = schema.Select(s =>
            {
                userMap.TryGetValue(s.Name, out var state);
                global.TryGetValue(s.Name, out var gp);
                return new AchievementDto
                {
                    ApiName = s.Name,
                    DisplayName = s.DisplayName,
                    Description = s.Description, // may be null for hidden until earned
                    Achieved = state.Item1,
                    UnlockTime = state.Item2.HasValue ? DateTimeOffset.FromUnixTimeSeconds(state.Item2.Value) : null,
                    Icon = s.Icon,
                    IconGray = s.IconGray,
                    GlobalPercent = gp
                };
            })
            .OrderByDescending(a => a.Achieved)
            .ThenBy(a => a.DisplayName)
            .ToList();

            return new GameDetailsDto
            {
                AppId = appId,
                Name = store?.Name ?? schemaTask.Result.Game?.GameName,
                HeaderImage = store?.HeaderImage,
                ShortDescription = store?.ShortDescription,
                DetailedDescription = Sanitize(store?.DetailedDescription),
                AboutTheGame = Sanitize(store?.AboutTheGame),
                Website = store?.Website,
                Developers = store?.Developers,
                Publishers = store?.Publishers,
                Genres = store?.Genres?.Select(g => g.Description).ToArray(),
                CurrentPlayers = ccu,
                Achievements = achievements
            };
        }

        private async Task<PlayerAchievementsResponse?> TryGetPlayerAchievementsAsync(string steamId64, int appId, string lang = "english")
        {
            var req = "https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/"
                .SetQueryParams(new { key = _apiKey, steamid = steamId64, appid = appId, l = lang });

            var resp = await req.AllowAnyHttpStatus().GetAsync();

            // Success → parse to typed response
            if (resp.ResponseMessage.IsSuccessStatusCode)
                return await resp.GetJsonAsync<PlayerAchievementsResponse>();

            // 400/401/403 happen for private profiles or no stats
            if (resp.StatusCode is (int)HttpStatusCode.BadRequest
                               or (int)HttpStatusCode.Unauthorized
                               or (int)HttpStatusCode.Forbidden)
            {
                var text = await resp.GetStringAsync();

                // Try to read Steam's error to decide what to do
                try
                {
                    var err = JsonSerializer.Deserialize<PlayerAchievementsErrorEnvelope>(text);
                    var msg = err?.Playerstats?.Error ?? "Unknown";
                    // Log msg if you have logging; decide how you want to represent this in your response
                    // e.g., return null and let the caller handle "no achievements available"
                    return null;
                }
                catch
                {
                    // If body isn't the expected JSON, also just treat as no data
                    return null;
                }
            }

            // Other statuses → bubble up so you can see them during dev/monitoring
            resp.ResponseMessage.EnsureSuccessStatusCode();
            return null; // not reached
        }

        static async Task<GlobalPercentResponse?> GetGlobalAsync(int appId)
        {
            var resp = await "https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/"
                .SetQueryParams(new { gameid = appId })
                .AllowAnyHttpStatus()
                .GetAsync();

            if (resp.ResponseMessage.IsSuccessStatusCode)
                return await resp.GetJsonAsync<GlobalPercentResponse>(); // non-null on success

            // treat these as "no data" and proceed
            if (resp.StatusCode is (int)HttpStatusCode.Unauthorized
                               or (int)HttpStatusCode.Forbidden
                               or (int)HttpStatusCode.NotFound)
                return null;

            // otherwise bubble up (so you can see unexpected failures)
            resp.ResponseMessage.EnsureSuccessStatusCode();
            return null;
        }
    }
}


