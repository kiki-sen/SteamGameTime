using Flurl.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steam_API.Contracts;
using Steam_API.Dto.Output;
using Steam_API.Services;
using Swashbuckle.AspNetCore.Annotations;
namespace Steam_API.Controllers
{
    /// <summary>Steam data endpoints (require JWT).</summary>
    [ApiController]
    [Route("api/steam")]
    public class SteamController(SteamApiClient client, SteamGameService steamGameService) : ControllerBase
    {
        private static string? BuildSteamIconUrl(int appId, string? imgIconHash)
            => string.IsNullOrWhiteSpace(imgIconHash)
               ? null
               : $"https://media.steampowered.com/steamcommunity/public/images/apps/{appId}/{imgIconHash}.jpg";

        private static string? BuildSteamLogoUrl(int appId, string? imgLogoHash)
            => string.IsNullOrWhiteSpace(imgLogoHash)
               ? null
               : $"https://media.steampowered.com/steamcommunity/public/images/apps/{appId}/{imgLogoHash}.jpg";

        private void SetIconAndLogoUrls(GameHoursDto game)
        {
            game.img_logo_url = BuildSteamLogoUrl(game.AppId, game.img_icon_url);
            game.img_icon_url = BuildSteamIconUrl(game.AppId, game.img_icon_url);
        }

        /// <summary>Get owned games with paging, search and sorting.</summary>
        /// <remarks>Requires Bearer JWT. Steam privacy must allow game details.</remarks>
        /// <param name="query">Paging/filter/sort query:
        /// page (≥1), pageSize (1–200), q (search name), sort = name|hoursTotal|hours2w[:desc]</param>
        [HttpGet("games")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [SwaggerOperation(Summary = "Owned games", Description = "Returns all owned games for the authenticated user.")]
        [ProducesResponseType(typeof(IEnumerable<GameDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<GameHoursDto>>> GetGames([FromQuery] GamesQuery query)
        {
            var steamId = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(steamId))
            {
                return Unauthorized();
            }

            var all = await client.GetGamePlayTimeAsync(steamId);
            all.ToList().ForEach(SetIconAndLogoUrls);

            // Filter
            IEnumerable<GameHoursDto> filtered = all;
            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var q = query.Q.Trim();
                filtered = filtered.Where(g => g.Name != null && g.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            // Sort
            var sort = query.Sort?.Trim();
            bool desc = sort?.EndsWith(":desc", StringComparison.OrdinalIgnoreCase) == true;
            var key = (sort ?? "hoursTotal:desc").Split(':', 2)[0].ToLowerInvariant();

            Func<GameHoursDto, object> keySelector = key switch
            {
                "name" => g => g.Name ?? string.Empty,
                "hourstotal" => g => g.HoursTotal,
                "hours2w" => g => g.Hours2Weeks,
                _ => g => g.HoursTotal
            };

            filtered = desc
                ? filtered.OrderByDescending(keySelector).ThenBy(g => g.Name)
                : filtered.OrderBy(keySelector).ThenBy(g => g.Name);

            // Page
            var total = filtered.Count();
            var skip = (query.Page - 1) * query.PageSize;
            var items = filtered.Skip(skip).Take(query.PageSize).ToList();

            var result = new PageResult<GameHoursDto>
            {
                Items = items,
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize,
                Sort = sort ?? "hoursTotal:desc",
                Q = query.Q
            };

            return Ok(result);
        }

        /// <summary>
        /// Get detailed information about a game, including the full achievement list,
        /// which achievements the signed-in user has unlocked, unlock times, and icons.
        /// Optionally includes global completion percentages.
        /// </summary>
        /// <param name="appid">Steam AppID of the game.</param>
        /// <param name="includeGlobal">Include global achievement percentages.</param>
        /// <param name="lang">Response language (e.g., "english", "schinese").</param>
        [HttpGet("{appid:int}/gamedetails")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [SwaggerOperation(
            Summary = "Get rich game details (achievements + user progress)",
            Description = "Joins Steam schema (icons/labels), user achievements (unlock state/time), "
                        + "and store metadata (header image). Uses the signed-in user's SteamID.",
            OperationId = "GetGameDetails",
            Tags = new[] { "Games" }
        )]
        [ProducesResponseType(typeof(GameDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GameDetailsDto>> GetGameDetails(
            [FromRoute] int appid,
            [FromQuery] bool includeGlobal = true,
            [FromQuery] string lang = "english")
        {
            var steamId = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(steamId))
            {
                return Unauthorized();
            }

            try
            {
                var dto = await steamGameService.GetGameDetailsAsync(steamId, appid, includeGlobal, lang);
                return Ok(dto);
            }
            catch (FlurlHttpException ex) when ((int?)ex.Call?.Response?.StatusCode is 401 or 403)
            {
                // Private profile/game stats or access denied by Steam for this user/game
                return Problem(
                    title: "Forbidden",
                    detail: "Achievements/stats are private or unavailable for this game/user.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Unexpected error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}


