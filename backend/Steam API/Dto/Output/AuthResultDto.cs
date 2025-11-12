namespace Steam_API.Dto.Output
{
    /// <summary>Authentication result returned to SPA.</summary>
    public class AuthResultDto
    {
        /// <summary>Issued JWT for API access.</summary>
        public string token { get; set; } = string.Empty;
        /// <summary>Refresh token for obtaining new access tokens.</summary>
        public string refreshToken { get; set; } = string.Empty;
        /// <summary>Authenticated SteamID64.</summary>
        public string steamid { get; set; } = string.Empty;
    }

}


