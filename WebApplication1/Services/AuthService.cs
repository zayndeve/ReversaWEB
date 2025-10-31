using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AuthService
    {
        private readonly string _secretToken;
        private readonly int _authTimer; // hours

        public AuthService()
        {
            // Load from environment or fallback
            _secretToken = Environment.GetEnvironmentVariable("SECRET_TOKEN")
                ?? "super_secret_secure_key_12345678901234567890";

            _authTimer = int.TryParse(Environment.GetEnvironmentVariable("AUTH_TIMER"), out var hours)
                ? hours
                : 12;
        }

        /// <summary>
        /// Create JWT token for a given member payload.
        /// </summary>
        public string CreateToken(MemberTokenPayload payload)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretToken);

            var claims = new[]
            {
                new Claim("Id", payload.Id.ToString()),
                new Claim("MemberNick", payload.MemberNick ?? string.Empty),
                new Claim("MemberEmail", payload.MemberEmail ?? string.Empty),
                new Claim("MemberStatus", payload.MemberStatus.ToString()),
                new Claim("MemberType", payload.MemberType.ToString()),
                new Claim("MemberImage", payload.MemberImage ?? string.Empty),
                new Claim("MemberAddress", payload.MemberAddress ?? string.Empty),
                new Claim("MemberPhone", payload.MemberPhone ?? string.Empty),
                new Claim("MemberPoints", payload.MemberPoints?.ToString() ?? "0")
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_authTimer),
                signingCredentials: credentials
            );

            return handler.WriteToken(token);
        }

        /// <summary>
        /// Verify and decode JWT token; return the payload if valid.
        /// </summary>
        public MemberTokenPayload? CheckAuth(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretToken);

            try
            {
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwt = (JwtSecurityToken)validatedToken;

                return new MemberTokenPayload
                {
                    Id = Guid.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == "Id")?.Value, out var guid)
    ? guid.ToString()
    : string.Empty,
                    MemberNick = jwt.Claims.FirstOrDefault(c => c.Type == "MemberNick")?.Value ?? string.Empty,
                    MemberEmail = jwt.Claims.FirstOrDefault(c => c.Type == "MemberEmail")?.Value,
                    MemberStatus = Enum.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == "MemberStatus")?.Value, out Enums.MemberStatus status)
    ? status
    : Enums.MemberStatus.Active,
                    MemberType = Enum.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == "MemberType")?.Value, out Enums.MemberType type)
    ? type
    : Enums.MemberType.User,
                    MemberImage = jwt.Claims.FirstOrDefault(c => c.Type == "MemberImage")?.Value,
                    MemberAddress = jwt.Claims.FirstOrDefault(c => c.Type == "MemberAddress")?.Value,
                    MemberPhone = jwt.Claims.FirstOrDefault(c => c.Type == "MemberPhone")?.Value,
                    MemberPoints = int.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == "MemberPoints")?.Value, out var pts) ? pts : 0

                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AuthService.CheckAuth error: {ex.Message}");
                return null;
            }
        }
    }
}
