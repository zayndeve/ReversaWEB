using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ReversaWEB.Core.Types;
using ReversaWEB.Models;

namespace ReversaWEB.Services
{
    public class AuthService
    {
        private readonly string _secretToken;
        private readonly int _authTimer; // in hours

        public AuthService()
        {
            // You can set this in appsettings.json or environment variable
            _secretToken = Environment.GetEnvironmentVariable("SECRET_TOKEN")
    ?? "super_secret_secure_key_12345678901234567890";
            _authTimer = int.TryParse(Environment.GetEnvironmentVariable("AUTH_TIMER"), out var hours) ? hours : 12;
        }

        /// <summary>
        /// Creates a JWT token for the given member.
        /// </summary>
        public string CreateToken(MemberTokenPayload payload)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretToken);

            var claims = new[]
            {
                new Claim("Id", payload.Id ?? string.Empty),
                new Claim("MemberNick", payload.MemberNick ?? string.Empty),
                new Claim("MemberEmail", payload.MemberEmail ?? string.Empty),
                new Claim("MemberStatus", payload.MemberStatus ?? string.Empty),
                new Claim("MemberType", payload.MemberType ?? string.Empty),
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
        /// Verifies and decodes a JWT token, returning the member payload.
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
                    Id = jwt.Claims.FirstOrDefault(c => c.Type == "Id")?.Value ?? string.Empty,
                    MemberNick = jwt.Claims.FirstOrDefault(c => c.Type == "MemberNick")?.Value ?? string.Empty,
                    MemberEmail = jwt.Claims.FirstOrDefault(c => c.Type == "MemberEmail")?.Value,
                    MemberStatus = jwt.Claims.FirstOrDefault(c => c.Type == "MemberStatus")?.Value ?? "ACTIVE",
                    MemberType = jwt.Claims.FirstOrDefault(c => c.Type == "MemberType")?.Value ?? "MEMBER",
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
