using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Steam_API.Dto.Output;
using Steam_API.Dto.Input;

namespace Steam_API.Services
{
    public class SteamApiClient(IConfiguration cfg)
    {
        private readonly string _apiKey = (cfg ?? throw new ArgumentNullException(nameof(cfg)))["Steam:ApiKey"] ?? throw new Exception("Steam:ApiKey missing");

        private async Task<List<GameDto>> GetOwnedGamesAsync(string steamId)
        {
            var data = await "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/"
                .SetQueryParams(new
                {
                    key = _apiKey,
                    steamid = steamId,
                    include_appinfo = 1,
                    include_played_free_games = 1,
                    format = "json"
                })
                .WithTimeout(TimeSpan.FromSeconds(12))
                .GetJsonAsync<OwnedGamesResponse>();

            return data.response.games?.Select(g => new GameDto
            {
                appid = g.appid,
                name = g.name ?? string.Empty,
                img_icon_url = g.img_icon_url,
                playtime_forever = g.playtime_forever
            }).ToList() ?? [];
        }

        private async Task<RecentlyPlayedResponse> GetRecentlyPlayedAsync(string steamId)
        {
            return await "https://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v1/"
                .SetQueryParams(new
                {
                    key = _apiKey,
                    steamid = steamId
                })
                .WithTimeout(TimeSpan.FromSeconds(12))
                .GetJsonAsync<RecentlyPlayedResponse>();
        }

        public async Task<IEnumerable<GameHoursDto>> GetGamePlayTimeAsync(string steamId)
        {
            var ownedGames = await GetOwnedGamesAsync(steamId);
            var recentGames = await GetRecentlyPlayedAsync(steamId);

            var recentByApp = recentGames.Response.Games.ToDictionary(g => g.AppId, g => g.Playtime2Weeks ?? 0);

            return ownedGames
                .Select(g => new GameHoursDto()
                {
                    AppId = g.appid,
                    Name = g.name ?? string.Empty,
                    HoursTotal = g.playtime_forever / 60.0,
                    Hours2Weeks = (recentByApp.TryGetValue((uint)g.appid, out var mins) ? mins : 0) / 60.0,
                    img_icon_url = g.img_icon_url
                })
                .OrderByDescending(x => x.HoursTotal)
                .ToList();
        }
    }
}



