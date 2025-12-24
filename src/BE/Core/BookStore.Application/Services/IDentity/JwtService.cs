using BookStore.Application.IService.Identity;
using BookStore.Application.Services.Common;
using BookStore.Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.IDentity
{
    public class JwtService : IJwtService
    {
        private readonly JwtOptions _opts;
        public JwtService(IOptions<JwtOptions> opts) { _opts = opts.Value; }

        public string GenerateAccessToken(User user, IEnumerable<string>? roles = null)
        {
            var claims = new List<Claim>
    {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
    };

            // Fix crash when roles = null
            var safeRoles = roles ?? Enumerable.Empty<string>();
            claims.AddRange(safeRoles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _opts.Issuer,
                audience: _opts.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public (string token, DateTime expiresAt) GenerateRefreshToken()
        {
            var random = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(random);
            var token = Convert.ToBase64String(random);
            var expires = DateTime.UtcNow.AddDays(_opts.RefreshTokenDays);
            return (token, expires);
        }

        public ClaimsPrincipal? ValidateAccessToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var tokenParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _opts.Issuer,
                ValidateAudience = true,
                ValidAudience = _opts.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key)),
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = handler.ValidateToken(token, tokenParams, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
