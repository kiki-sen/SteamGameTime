using AspNet.Security.OpenId.Steam;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steam_API.Dto.Input;
using Steam_API.Dto.Output;
using Steam_API.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Steam_API.Controllers
{
    /// <summary>Authentication endpoints for Steam OpenID and SPA helpers.</summary>
    [ApiController]
    [Route("auth/steam")]
    public class SteamAuthController(IJwtTokenService jwtSvc, IRefreshTokenStore refreshTokenStore, IConfiguration cfg) : ControllerBase
    {
        static string? ExtractSteamId(ClaimsPrincipal? user)
        {
            if (user is null)
            {
                return null;
            }

            // First try a pure numeric claim
            var val = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirst("steamid")?.Value
                     ?? user.Claims.FirstOrDefault(c =>
                            c.Type.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase))?.Value;

            if (string.IsNullOrWhiteSpace(val))
            {
                return null;
            }

            if (Regex.IsMatch(val, @"^\d{17}$"))
            {
                return val;
            }

            // If it's a URL like https://steamcommunity.com/openid/id/7656...
            var m = Regex.Match(val, @"(\d{17})$");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>Starts the Steam login flow.</summary>
        /// <remarks>Redirects the browser to steamcommunity.com for OpenID authentication.</remarks>
        [HttpGet("login")]
        [SwaggerOperation(Summary = "Initiate Steam login", Description = "Redirects to Steam OpenID.")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public IActionResult Login()
            => Challenge(new AuthenticationProperties { RedirectUri = Url.Action("callback")! }, SteamAuthenticationDefaults.AuthenticationScheme);

        /// <summary>Callback from Steam after login.</summary>
        /// <remarks>Exchanges the Steam identity for a JWT and redirects the SPA with <c>?token=...</c>.</remarks>
        [HttpGet("callback")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [SwaggerOperation(Summary = "Steam callback", Description = "Creates a JWT then redirects the SPA.")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Callback()
        {
            var steamId = ExtractSteamId(User);
            if (string.IsNullOrEmpty(steamId))
            {
                return BadRequest();
            }

            var token = jwtSvc.CreateToken(steamId);
            var refreshToken = refreshTokenStore.CreateRefreshToken(steamId);
            var front = cfg["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
            return Redirect($"{front}/auth/callback?token={Uri.EscapeDataString(token)}&refreshToken={Uri.EscapeDataString(refreshToken)}");
        }

        /// <summary>Returns the current authenticated user (JWT required).</summary>
        [HttpGet("me")]
        [SwaggerOperation(Summary = "Who am I?", Description = "Returns minimal profile for the current JWT")]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Me()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }

            return Ok(new ProfileDto() 
            {
                SteamId64 = User.FindFirstValue("steamId") ?? string.Empty
            });
        }

        /// <summary>Get JWT token for authenticated Steam user.</summary>
        /// <remarks>Call this after successful Steam login to get a JWT token.</remarks>
        [HttpGet("token")]
        [SwaggerOperation(Summary = "Get JWT token", Description = "Returns JWT token for authenticated Steam user.")]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetToken()
        {
            Console.WriteLine($"[GetToken] IsAuth={User?.Identity?.IsAuthenticated}, AuthType={User?.Identity?.AuthenticationType}");
            
            var steamId = ExtractSteamId(User);
            Console.WriteLine($"[GetToken] Extracted SteamId={steamId}");

            if (string.IsNullOrEmpty(steamId))
            {
                return Unauthorized("Steam authentication required");
            }

            var token = jwtSvc.CreateToken(steamId);
            var refreshToken = refreshTokenStore.CreateRefreshToken(steamId);
            return Ok(new AuthResultDto { token = token, refreshToken = refreshToken, steamid = steamId });
        }

        /// <summary>Refresh access token using refresh token.</summary>
        [HttpPost("refresh")]
        [SwaggerOperation(Summary = "Refresh token", Description = "Get new access token using refresh token.")]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Refresh([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return Unauthorized("Refresh token is required");
            }

            var refreshToken = refreshTokenStore.GetRefreshToken(request.RefreshToken);
            if (refreshToken == null)
            {
                return Unauthorized("Invalid or expired refresh token");
            }

            var newAccessToken = jwtSvc.CreateToken(refreshToken.SteamId);
            var newRefreshToken = refreshTokenStore.CreateRefreshToken(refreshToken.SteamId);

            refreshTokenStore.RevokeRefreshToken(request.RefreshToken);

            return Ok(new AuthResultDto 
            { 
                token = newAccessToken, 
                refreshToken = newRefreshToken, 
                steamid = refreshToken.SteamId 
            });
        }

        /// <summary>Logs out the current authenticated user.</summary>
        [HttpPost("logout")]
        [SwaggerOperation(Summary = "Log out", Description = "Logs out the current user")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
        {
            if (request?.RefreshToken != null)
            {
                refreshTokenStore.RevokeRefreshToken(request.RefreshToken);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }
    }
}

