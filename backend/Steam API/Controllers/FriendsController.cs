using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steam_API.Services;
using Swashbuckle.AspNetCore.Annotations;
using Steam_API.Dto.Output;

namespace Steam_API.Controllers
{
    [ApiController]
    [Route("api/steam/friends")]
    public class FriendsController(IFriendsService svc, IConfiguration cfg) : ControllerBase
    {
        [HttpGet("leaderboard")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [SwaggerOperation(
            Summary = "Friends leaderboard (hours)",
            Description = "Ranks your Steam friends by playtime. If appid is provided, ranks by hours for that game; otherwise by total library hours."
        )]
        [ProducesResponseType(typeof(FriendsLeaderboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<FriendsLeaderboardDto>> Leaderboard(
            [FromQuery] int? appid = null,
            CancellationToken ct = default)
        {
            var apiKey = cfg["Steam:WebApiKey"]!;
            var me = User.FindFirst("steamId")?.Value
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(me)) return Unauthorized();

            var dto = await svc.GetLeaderboardAsync(me, appid, ct);
            return Ok(dto);
        }

        /// <summary>Gets your Steam friends (summaries), optionally including yourself.</summary>
        [HttpGet("list")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [SwaggerOperation(
            Summary = "Friends list",
            Description = "Returns persona/avatars for all friends. Supports search and paging."
        )]
        [ProducesResponseType(typeof(FriendsListDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<FriendsListDto>> List(
            [FromQuery] bool includeSelf = true,
            CancellationToken ct = default)
        {
            var apiKey = cfg["Steam:WebApiKey"]!;
            var me = User.FindFirst("steamId")?.Value
                  ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(me)) return Unauthorized();

            var dto = await svc.GetFriendsListAsync(me, includeSelf, ct);
            return Ok(dto);
        }
    }
}


