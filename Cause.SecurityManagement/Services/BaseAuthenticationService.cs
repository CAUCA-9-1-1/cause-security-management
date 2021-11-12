using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cause.SecurityManagement.Services
{
    public abstract class BaseAuthenticationService<TUser>
        where TUser : User, new()
    {
        protected readonly SecurityConfiguration securityConfiguration;
        protected readonly ISecurityContext<TUser> context;

        public readonly int DefaultRefreshTokenLifetimeInMinutes = 9 * 60;
        public readonly int DefaultAccessTokenLifetimeInMinutes = 60;
        public readonly int DefaultTemporaryAccessTokenLifetimeInMinutes = 5;

        protected BaseAuthenticationService(
           ISecurityContext<TUser> context,
           IOptions<SecurityConfiguration> securityOptions)
        {
            this.context = context;
            securityConfiguration = securityOptions.Value;
        }

        public void ThrowExceptionWhenTokenIsNotValid(string refreshToken, BaseToken token)
        {
            if (token == null)
                throw new SecurityTokenException("Invalid token.");

            if (token.RefreshToken != refreshToken)
                throw new SecurityTokenValidationException("Invalid token.");

            if (securityConfiguration.RefreshTokenCanExpire && token.ExpiresOn < DateTime.Now)
                throw new SecurityTokenExpiredException("Token expired.");
        }

        protected Guid GetSidFromExpiredToken(string token)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var id = principal.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;
            if (Guid.TryParse(id, out Guid userId))
                return userId;
            return Guid.Empty;
        }

        protected ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityConfiguration.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = securityConfiguration.Issuer,
                ValidateAudience = true,
                ValidAudience = securityConfiguration.PackageName,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (!(securityToken is JwtSecurityToken jwtSecurityToken)
                || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        protected string GenerateAccessToken(Guid userId, string userName, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
                new Claim(JwtRegisteredClaimNames.Sid, userId.ToString()),
            };

            return GenerateAccessToken(claims);
        }

        protected string GenerateAccessToken(Claim[] claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityConfiguration.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(securityConfiguration.Issuer,
                securityConfiguration.PackageName,
                claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddMinutes(GetAccessTokenLifeTimeInMinute()),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public int GetRefreshTokenLifeTimeInMinute()
        {
            return securityConfiguration.RefreshTokenLifeTimeInMinutes ?? DefaultRefreshTokenLifetimeInMinutes;
        }

        public int GetTemporaryAccessTokenLifeTimeInMinute()
        {
            return securityConfiguration.TemporaryAccessTokenLifeTimeInMinutes ?? DefaultTemporaryAccessTokenLifetimeInMinutes;
        }

        public int GetAccessTokenLifeTimeInMinute()
        {
            return securityConfiguration.AccessTokenLifeTimeInMinutes ?? DefaultAccessTokenLifetimeInMinutes;
        }
    }
}