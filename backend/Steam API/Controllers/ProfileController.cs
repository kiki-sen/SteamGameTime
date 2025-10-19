using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steam_API.Dto.Output;
using Steam_API.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Steam_API.Controllers
{
    [ApiController]
    [Route("api/steam")]
    [Produces("application/json")]
    public class ProfileController(ISteamProfileService svc) : ControllerBase
    {
        /// <summary>Returns the signed-in user’s Steam profile (name, avatars, level, visibility).</summary>
        [HttpGet("profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [SwaggerOperation(
            Summary = "Get my Steam profile",
            Description = "Fetches persona, avatars, level, country, and visibility for the current user.",
            OperationId = "GetMyProfile",
            Tags = new[] { "Profile" })]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProfileDto>> GetMyProfile(CancellationToken ct)
        {
            var steamId = User.FindFirst("steamId")?.Value
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(steamId))
            {
                return Unauthorized();
            }

            try
            {
                var dto = await svc.GetProfileAsync(steamId, ct);
                return Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Profile not available",
                    Detail = ex.Message
                });
            }
        }
    }
}


