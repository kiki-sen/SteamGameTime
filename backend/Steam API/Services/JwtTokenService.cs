using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Steam_API.Services
{
    public interface IJwtTokenService
    {
        string CreateToken(string steamId);
    }

    public class JwtTokenService(SymmetricSecurityKey signingKey, IConfiguration cfg) : IJwtTokenService
    {
        public virtual string CreateToken(string steamId)
        {
            var jwt = cfg.GetSection("Jwt");
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, steamId),
                new Claim(ClaimTypes.Name, steamId),
                new Claim("steamId", steamId)
            };

            var accessTokenLifetimeMinutes = cfg.GetValue<int>("Jwt:AccessTokenLifetimeMinutes", 60);

            var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(accessTokenLifetimeMinutes),
            signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

