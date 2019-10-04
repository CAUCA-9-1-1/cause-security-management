using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cause.SecurityManagement.Services
{
	public class AuthenticationService<TUser> : IAuthentificationService
        where TUser : User, new()
    {
		private readonly ISecurityContext<TUser> context;
        private readonly int refreshLifetime = 9 * 60;
        private readonly int tokenLifetime = 60;

        public AuthenticationService(ISecurityContext<TUser> context, IConfiguration configuration)
		{
			this.context = context;

            var lifetime = configuration.GetSection("APIConfig:TokenMinutesLifetime").Value;
            if (!string.IsNullOrEmpty(lifetime))
            {
                tokenLifetime = int.Parse(lifetime);
            }
		}

		public (UserToken token, User user) Login(string userName, string password, string applicationName, string issuer, string secretKey)
		{
			var encodedPassword = new PasswordGenerator().EncodePassword(password, applicationName);
			var userFound = context.Users
				.SingleOrDefault(user => user.UserName == userName && user.Password.ToUpper() == encodedPassword && user.IsActive);
			if (userFound != null)
			{
				var accessToken = GenerateAccessTokenForUser(userFound, applicationName, issuer, secretKey);
				var refreshToken = GenerateRefreshToken();
				var token = new UserToken {AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = DateTime.Now.AddMinutes(refreshLifetime), IdUser = userFound.Id};
				context.Add(token);
				context.SaveChanges();
				return (token, userFound);
			}

			return (null, null);
		}

        public (ExternalSystemToken token, ExternalSystem system) LoginForExternalSystem(string secretApiKey, string applicationName, string issuer, string secretKey)
        {
            var externalSystemFound = context.ExternalSystems
                .SingleOrDefault(externalSystem => externalSystem.ApiKey == secretApiKey && externalSystem.IsActive);
            if (externalSystemFound != null)
            {
                var accessToken = GenerateAccessTokenForExternalSystem(externalSystemFound, applicationName, issuer, secretKey);
                var refreshToken = GenerateRefreshToken();
                var token = new ExternalSystemToken { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = DateTime.Now.AddMinutes(refreshLifetime), IdExternalSystem = externalSystemFound.Id };
                context.Add(token);
                context.SaveChanges();
                return (token, externalSystemFound);
            }

            return (null, null);
        }

        public string RefreshUserToken(string token, string refreshToken, string applicationName, string issuer, string secretKey)
		{
			var userId = GetSidFromExpiredToken(token, issuer, applicationName, secretKey);
			var userToken = context.UserTokens
                .FirstOrDefault(t => t.IdUser == userId && t.RefreshToken == refreshToken);
		    var user = context.Users.Find(userId);

			ThrowExceptionWhenTokenIsNotValid(refreshToken, userToken);

            var newAccessToken = GenerateAccessTokenForUser(user, applicationName, issuer, secretKey);
            // ReSharper disable once PossibleNullReferenceException
            userToken.AccessToken = newAccessToken;
			context.SaveChanges();

			return newAccessToken;
		}

        public string RefreshExternalSystemToken(string token, string refreshToken, string applicationName, string issuer, string secretKey)
        {
            var externalSystemId = GetSidFromExpiredToken(token, issuer, applicationName, secretKey);
            var externalSystemToken = context.ExternalSystemTokens
                .FirstOrDefault(t => t.IdExternalSystem == externalSystemId && t.RefreshToken == refreshToken);
            var externalSystem = context.ExternalSystems.Find(externalSystemId);

            ThrowExceptionWhenTokenIsNotValid(refreshToken, externalSystemToken);

            var newAccessToken = GenerateAccessTokenForExternalSystem(externalSystem, applicationName, issuer, secretKey);
            // ReSharper disable once PossibleNullReferenceException
            externalSystemToken.AccessToken = newAccessToken;
            context.SaveChanges();

            return newAccessToken;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void ThrowExceptionWhenTokenIsNotValid(string refreshToken, BaseToken token)
        {
            if (token == null)
                throw new SecurityTokenException("Invalid token.");

            if (token.RefreshToken != refreshToken)
                throw new SecurityTokenValidationException("Invalid token.");

            if (token.ExpiresOn < DateTime.Now)
                throw new SecurityTokenExpiredException("Token expired.");
        }

        private Guid GetSidFromExpiredToken(string token, string issuer, string appName, string secretKey)
		{
			var principal = GetPrincipalFromExpiredToken(token, issuer, appName, secretKey);
			var id = principal.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;
			if (Guid.TryParse(id, out Guid userId))
				return userId;
			return Guid.Empty;
		}

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token, string issuer, string appName, string secretKey)
		{

            var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
				ValidateIssuer = true,
				ValidIssuer = issuer,
				ValidateAudience = true,
				ValidAudience = appName,
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

		private string GenerateAccessTokenForUser(User userLoggedIn, string applicationName, string issuer, string secretKey)
        {
            var claims = new[]
			{
                new Claim(ClaimTypes.Role, SecurityRoles.User),
                new Claim(JwtRegisteredClaimNames.UniqueName, userLoggedIn.UserName),
				new Claim(JwtRegisteredClaimNames.Sid, userLoggedIn.Id.ToString()),
			};

            return GenerateAccessToken(applicationName, issuer, secretKey, claims);
        }

        private string GenerateAccessTokenForExternalSystem(ExternalSystem externalSystem, string applicationName, string issuer, string secretKey)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, SecurityRoles.ExternalSystem),
                new Claim(JwtRegisteredClaimNames.UniqueName, externalSystem.ApiKey),
                new Claim(JwtRegisteredClaimNames.Sid, externalSystem.Id.ToString()),
            };

            return GenerateAccessToken(applicationName, issuer, secretKey, claims);
        }

        private string GenerateAccessToken(string applicationName, string issuer, string secretKey, Claim[] claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(issuer,
                applicationName,
                claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddMinutes(tokenLifetime),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
		{
			var randomNumber = new byte[32];
			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(randomNumber);
				return Convert.ToBase64String(randomNumber);
			}
		}

		public void EnsureAdminIsCreated(string applicationName)
		{
			if (!context.Users.Any(user => user.UserName == "admin"))
			{
				var user = new TUser
				{
					Email = "dev@cauca.ca",
					FirstName = "Admin",
					LastName = "Cauca",
					UserName = "admin",
                    IsActive = true,
					Password = new PasswordGenerator().EncodePassword("admincauca", applicationName)
				};
				context.Add(user);
				context.SaveChanges();
			}
		}

		public bool IsMobileVersionValid(string mobileVersion, string minimalVersion)
		{
			var mobile = new Version(mobileVersion);
			var minVersion = new Version(minimalVersion);

			if (mobile.CompareTo(minVersion) < 0)
				return false;
			return true;
		}

        public List<AuthentificationUserPermission> GetActiveUserPermissions(Guid userId)
        {
            var idGroups = context.UserGroups
                .Where(ug => ug.IdUser == userId)
                .Select(ug => ug.IdGroup);
            var restrictedPermissions = context.GroupPermissions
                .Where(g => idGroups.Contains(g.IdGroup) && g.IsAllowed == false)
                .Include(g => g.Permission)
                .Select(p => new AuthentificationUserPermission
                {
                    IdModulePermission = p.IdModulePermission,
                    Tag = p.Permission.Tag,
                    IsAllowed = p.IsAllowed,
                });
            var allowedPermissions = context.GroupPermissions
                .Where(g => idGroups.Contains(g.IdGroup) && g.IsAllowed && !restrictedPermissions
                                .Select(p => p.IdModulePermission).Contains(g.IdModulePermission))
                .Include(g => g.Permission)
                .Select(p => new AuthentificationUserPermission
                {
                    IdModulePermission = p.IdModulePermission,
                    Tag = p.Permission.Tag,
                    IsAllowed = p.IsAllowed,
                })
                .Distinct();

            return restrictedPermissions.Concat(allowedPermissions).ToList();
        }
    }
}