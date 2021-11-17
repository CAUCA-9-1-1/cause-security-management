using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cause.SecurityManagement.Services
{
    public class TokenGenerator : ITokenGenerator
    {
        public readonly int DefaultRefreshTokenLifetimeInMinutes = 9 * 60;
        public readonly int DefaultAccessTokenLifetimeInMinutes = 60;
        public readonly int DefaultTemporaryAccessTokenLifetimeInMinutes = 5;

        private readonly SecurityConfiguration configuration;

        public TokenGenerator(IOptions<SecurityConfiguration> configuration)
        {
            this.configuration = configuration.Value;
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public string GenerateAccessToken(Guid userId, string userName, string role)
        {
            var lifeTimeInMinute = SecurityRoles.IsTemporaryRole(role) ? GetTemporaryAccessTokenLifeTimeInMinute() : GetAccessTokenLifeTimeInMinute();

            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
                new Claim(JwtRegisteredClaimNames.Sid, userId.ToString()),
            };

            return GenerateAccessToken(claims, lifeTimeInMinute);
        }

        public DateTime GenerateAccessExpirationDateByRole(string role)
        {
            var lifeTimeInMinute = SecurityRoles.IsTemporaryRole(role) ? GetTemporaryAccessTokenLifeTimeInMinute() : GetAccessTokenLifeTimeInMinute();
            return DateTime.Now.AddMinutes(lifeTimeInMinute);
        }

        protected string GenerateAccessToken(Claim[] claims, int lifeTimeInMinute)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(configuration.Issuer,
                configuration.PackageName,
                claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddMinutes(lifeTimeInMinute),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int GetRefreshTokenLifeTimeInMinute()
        {
            return configuration.RefreshTokenLifeTimeInMinutes ?? DefaultRefreshTokenLifetimeInMinutes;
        }

        public int GetTemporaryAccessTokenLifeTimeInMinute()
        {
            return configuration.TemporaryAccessTokenLifeTimeInMinutes ?? DefaultTemporaryAccessTokenLifetimeInMinutes;
        }

        public int GetAccessTokenLifeTimeInMinute()
        {
            return configuration.AccessTokenLifeTimeInMinutes ?? DefaultAccessTokenLifetimeInMinutes;
        }
    }
}
