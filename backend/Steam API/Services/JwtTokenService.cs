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
                new Claim(ClaimTypes.Name, steamId)
            };

            var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

