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

        public string GetSidFromExpiredToken(string token)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            return principal.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;
        }

        public string GetClaimValueFromExpiredToken(string token, string type)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var value = principal.Claims.FirstOrDefault(claim => claim.Type == type)?.Value;
            return value;
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

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token, bool isRetry = false)
        {
            try
            {
                var tokenValidationParameters = GenerateTokenValidationParameters(isRetry);
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                ThrowExceptionWhenInvalid(securityToken);

                return principal;
            }
            catch (SecurityTokenSignatureKeyNotFoundException exception)
            {
                if (!CanRetryWithPreviousSecretKey() || isRetry)
                {
                    throw new InvalidTokenException(token, exception);
                }
                return GetPrincipalFromExpiredToken(token, true);
            }
        }

        private bool CanRetryWithPreviousSecretKey()
        {
            return configuration.AllowTokenRefreshWithPreviousSecretKey && !string.IsNullOrWhiteSpace(configuration.PreviousSecretKey);
        }

        private static void ThrowExceptionWhenInvalid(SecurityToken securityToken)
        {
            if (IsInvalid(securityToken))
                throw new SecurityTokenException("Invalid token");
        }

        private static bool IsInvalid(SecurityToken securityToken)
        {
            return securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
        }

        private TokenValidationParameters GenerateTokenValidationParameters(bool isRetry)
        {
            var key = isRetry ? configuration.PreviousSecretKey : configuration.SecretKey;
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
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
