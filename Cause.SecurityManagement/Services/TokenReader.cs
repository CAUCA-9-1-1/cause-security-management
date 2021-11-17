using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Cause.SecurityManagement.Services
{
    public class TokenReader : ITokenReader
    {
        private readonly SecurityConfiguration configuration;

        public TokenReader(IOptions<SecurityConfiguration> configuration)
        {
            this.configuration = configuration.Value;
        }

        public Guid GetSidFromExpiredToken(string token)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var id = principal.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;
            if (Guid.TryParse(id, out Guid userId))
                return userId;
            return Guid.Empty;
        }

        public void ThrowExceptionWhenTokenIsNotValid(string refreshToken, BaseToken token)
        {
            if (token == null)
                throw new SecurityTokenException("Invalid token.");

            if (token.RefreshToken != refreshToken)
                throw new SecurityTokenValidationException("Invalid token.");

            if (configuration.RefreshTokenCanExpire && token.ExpiresOn < DateTime.Now)
                throw new SecurityTokenExpiredException("Token expired.");
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            TokenValidationParameters tokenValidationParameters = GenerateTokenValidationParameters();

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private TokenValidationParameters GenerateTokenValidationParameters()
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = configuration.Issuer,
                ValidateAudience = true,
                ValidAudience = configuration.PackageName,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };
        }
    }
}
