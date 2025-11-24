using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Ganss.Xss;
using Steam_API.Dto.Input;
using Steam_API.Dto.Output;
using System.Net;
using System.Text.Json;

namespace Steam_API.Services
{
    public interface ISteamGameService
    {
        Task<GameDetailsDto> GetGameDetailsAsync(string steamId64, int appId, bool includeGlobal = false, string lang = "english");
    }

    public sealed class SteamGameService(IConfiguration cfg, HtmlSanitizer sanitizer, ILogger<SteamGameService> logger, IFlurlClientCache clientCache, ISteamStoreFrontService steamappDetailsFrontService) : ISteamGameService
    {
        private readonly string _apiKey = cfg["Steam:ApiKey"] ?? throw new("Steam:ApiKey missing");
        private readonly IFlurlClient _client = clientCache.Get("steam-api");

        public string? Sanitize(string? html) => string.IsNullOrWhiteSpace(html) ? null : sanitizer.Sanitize(html);

        public async Task<GameDetailsDto> GetGameDetailsAsync(string steamId64, int appId, bool includeGlobal = false, string lang = "english")
        {
            // 1) Schema (achievement definitions + icons)
            Task<SchemaForGameResponse> schemaTask = GetSchemaForGame(appId, lang);

            // 2) User achievements (per-app)
            var userAchTask = TryGetPlayerAchievementsAsync(steamId64, appId, lang);

            // 3) appDetails appdetails (header image + name)
            var appDetails = await steamappDetailsFrontService.GetAppDetails(appId);

            // 4) Optional global achievements %
            var hasAchievements = (schemaTask.Result.Game?.AvailableGameStats?.Achievements?.Count ?? 0) > 0;
            Task<GlobalPercentResponse?> globalTask = (includeGlobal && hasAchievements)
                ? GetGlobalAsync(appId)
                : Task.FromResult<GlobalPercentResponse?>(null);

            // 5) CCU (current players)
            Task<CurrentPlayersResponse?> ccuTask = GetCurrentUsers(appId);

            await Task.WhenAll(schemaTask, userAchTask, globalTask);

            var schema = schemaTask.Result.Game?.AvailableGameStats?.Achievements ?? [];
            var userAch = userAchTask?.Result?.PlayerStats?.Achievements ?? [];
            var global = globalTask
                .Result?
                .Achievementpercentages?
                .Achievements?
                .ToDictionary(a => a.Name, a => a.Percent)
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
                Name = appDetails?.Name ?? schemaTask.Result.Game?.GameName,
                HeaderImage = appDetails?.HeaderImage,
                ShortDescription = appDetails?.ShortDescription,
                DetailedDescription = Sanitize(appDetails?.DetailedDescription),
                AboutTheGame = Sanitize(appDetails?.AboutTheGame),
                Website = appDetails?.Website,
                Developers = appDetails?.Developers,
                Publishers = appDetails?.Publishers,
                Genres = appDetails?.Genres?.Select(g => g.Description).ToArray(),
                CurrentPlayers = ccu,
                Achievements = achievements,
                Platforms = appDetails?.Platforms != null ? new Dto.Output.PlatformsDto()
                {
                    appId = appId,
                    linux = appDetails.Platforms.linux,
                    windows = appDetails.Platforms.windows,
                    mac = appDetails.Platforms.mac
                } : null,
            };
        }

        private Task<CurrentPlayersResponse?> GetCurrentUsers(int appId)
        {
            return _client
                .Request("ISteamUserStats", "GetNumberOfCurrentPlayers", "v1")
                .SetQueryParams(new
                {
                    appid = appId
                })
                .AllowAnyHttpStatus()
                .GetAsync()
                .ContinueWith(async t =>
                {
                    var resp = t.Result;
                    if (resp.ResponseMessage.IsSuccessStatusCode)
                    {
                        return await resp.GetJsonAsync<CurrentPlayersResponse>();
                    }

                    // treat 4xx/5xx as "no data"
                    return null;
                }).Unwrap();
        }

        private Task<SchemaForGameResponse> GetSchemaForGame(int appId, string lang)
        {
            return _client
                .Request("ISteamUserStats", "GetSchemaForGame", "v2")
                .SetQueryParams(new
                {
                    key = _apiKey,
                    appid = appId,
                    l = lang
                })
                .GetJsonAsync<SchemaForGameResponse>();
        }

        private async Task<PlayerAchievementsResponse?> TryGetPlayerAchievementsAsync(string steamId64, int appId, string lang = "english")
        {
            var playerAchievementsRequest = _client
                .Request("ISteamUserStats", "GetPlayerAchievements", "v1")
                .SetQueryParams(new 
                { 
                    key = _apiKey, 
                    steamid = steamId64, 
                    appid = appId, 
                    l = lang 
                });

            var response = await playerAchievementsRequest.AllowAnyHttpStatus().GetAsync();

            // Success → parse to typed response
            if (response.ResponseMessage.IsSuccessStatusCode)
            {
                return await response.GetJsonAsync<PlayerAchievementsResponse>();
            }

            // 400/401/403 happen for private profiles or no stats
            if (response.StatusCode is (int)HttpStatusCode.BadRequest
                               or (int)HttpStatusCode.Unauthorized
                               or (int)HttpStatusCode.Forbidden)
            {
                var text = await response.GetStringAsync();

                // Try to read Steam's error to decide what to do
                try
                {
                    var error = JsonSerializer.Deserialize<PlayerAchievementsErrorEnvelope>(text);
                    var message = error?.Playerstats?.Error ?? "Unknown error serializing PlayerAchievements";
                    logger.LogWarning(message);
                    return null;
                }
                catch
                {
                    // If body isn't the expected JSON, also just treat as no data
                    return null;
                }
            }

            // Other statuses → bubble up so you can see them during dev/monitoring
            response.ResponseMessage.EnsureSuccessStatusCode();
            return null;
        }

        private async Task<GlobalPercentResponse?> GetGlobalAsync(int appId)
        {
            var globalAchievementPerdcentages = await _client
                .Request("ISteamUserStats", "GetGlobalAchievementPercentagesForApp", "v0002")
                .SetQueryParams(new 
                { 
                    gameid = appId 
                })
                .AllowAnyHttpStatus()
                .GetAsync();

            if (globalAchievementPerdcentages.ResponseMessage.IsSuccessStatusCode)
            {
                return await globalAchievementPerdcentages.GetJsonAsync<GlobalPercentResponse>(); // non-null on success
            }

            // treat these as "no data" and proceed
            if (globalAchievementPerdcentages.StatusCode is (int)HttpStatusCode.Unauthorized
                               or (int)HttpStatusCode.Forbidden
                               or (int)HttpStatusCode.NotFound)
                return null;

            // otherwise bubble up (so you can see unexpected failures)
            globalAchievementPerdcentages.ResponseMessage.EnsureSuccessStatusCode();
            return null;
        }
    }
}
