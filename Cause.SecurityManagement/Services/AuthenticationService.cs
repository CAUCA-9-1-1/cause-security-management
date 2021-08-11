using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cause.SecurityManagement.Services
{
	public class AuthenticationService<TUser> : IAuthenticationService
        where TUser : User, new()
    {
        private readonly ICurrentUserService currentUserService;
        private readonly ISecurityContext<TUser> context;
        private readonly IUserManagementService<TUser> userManagementService;
        private readonly SecurityConfiguration securityConfiguration;
        public readonly int DefaultRefreshTokenLifetimeInMinutes = 9 * 60;
        public readonly int DefaultAccessTokenLifetimeInMinutes = 60;
        public readonly int DefaultTemporaryAccessTokenLifetimeInMinutes = 5;

        public AuthenticationService(
            ICurrentUserService currentUserService,
            ISecurityContext<TUser> context, 
            IUserManagementService<TUser> userManagementService,
            IOptions<SecurityConfiguration> securityOptions)
		{
            this.currentUserService = currentUserService;
            this.context = context;
            this.userManagementService = userManagementService;
            securityConfiguration = securityOptions.Value;
        }

		public (UserToken token, User user) Login(string userName, string password)
		{
			var encodedPassword = new PasswordGenerator().EncodePassword(password, securityConfiguration.PackageName);
			var userFound = context.Users
				.SingleOrDefault(user => user.UserName == userName && user.Password.ToUpper() == encodedPassword && user.IsActive);
			if (userFound != null && CanLogIn(userFound))
			{
				var accessToken = GenerateAccessToken(userFound.Id, userFound.UserName, SecurityRoles.User);
				var refreshToken = GenerateRefreshToken();
				var token = new UserToken {AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = DateTime.Now.AddMinutes(GetRefreshTokenLifeTimeInMinute()), IdUser = userFound.Id};
				context.Add(token);
				context.SaveChanges();
				return (token, userFound);
			}

			return (null, null);
		}

        private bool CanLogIn(User user)
        {
            return string.IsNullOrWhiteSpace(securityConfiguration.RequiredPermissionForLogin)
                || userManagementService.HasPermission(user.Id, securityConfiguration.RequiredPermissionForLogin);
        }

        public UserToken GenerateUserRecoveryToken(Guid userId)
        {
            var userFound = context.Users
                .SingleOrDefault(user => user.Id == userId && user.IsActive);
            if (userFound != null)
            {
                var accessToken = GenerateAccessToken(userFound.Id, userFound.UserName, SecurityRoles.UserRecovery);
                var token = new UserToken { AccessToken = accessToken, RefreshToken = "", ExpiresOn = DateTime.Now.AddMinutes(GetTemporaryAccessTokenLifeTimeInMinute()), IdUser = userFound.Id };
                context.Add(token);
                context.SaveChanges();
                return token;
            }
            return null;
        }

        public UserToken GenerateUserCreationToken(Guid userId)
        {            
            var accessToken = GenerateAccessToken(userId, "temporary", SecurityRoles.UserCreation);
            var token = new UserToken { AccessToken = accessToken, RefreshToken = "", ExpiresOn = DateTime.Now.AddMinutes(GetTemporaryAccessTokenLifeTimeInMinute()), IdUser = userId };
            return token;
        }

        public (ExternalSystemToken token, ExternalSystem system) LoginForExternalSystem(string secretApiKey)
        {
            var externalSystemFound = context.ExternalSystems
                .SingleOrDefault(externalSystem => externalSystem.ApiKey == secretApiKey && externalSystem.IsActive);
            if (externalSystemFound != null)
            {
                var accessToken = GenerateAccessToken(externalSystemFound.Id, externalSystemFound.Name, SecurityRoles.ExternalSystem);
                var refreshToken = GenerateRefreshToken();
                var token = new ExternalSystemToken { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = DateTime.Now.AddMinutes(GetRefreshTokenLifeTimeInMinute()), IdExternalSystem = externalSystemFound.Id };
                context.Add(token);
                context.SaveChanges();
                return (token, externalSystemFound);
            }

            return (null, null);
        }

        public string RefreshUserToken(string token, string refreshToken)
		{
			var userId = GetSidFromExpiredToken(token);
			var userToken = context.UserTokens
                .FirstOrDefault(t => t.IdUser == userId && t.RefreshToken == refreshToken);
		    var user = context.Users.Find(userId);

			ThrowExceptionWhenTokenIsNotValid(refreshToken, userToken);

            var newAccessToken = GenerateAccessToken(user.Id, user.UserName, SecurityRoles.User);
            // ReSharper disable once PossibleNullReferenceException
            userToken.AccessToken = newAccessToken;
			context.SaveChanges();

			return newAccessToken;
		}

        public string RefreshExternalSystemToken(string token, string refreshToken)
        {
            var externalSystemId = GetSidFromExpiredToken(token);
            var externalSystemToken = context.ExternalSystemTokens
                .FirstOrDefault(t => t.IdExternalSystem == externalSystemId && t.RefreshToken == refreshToken);
            var externalSystem = context.ExternalSystems.Find(externalSystemId);

            ThrowExceptionWhenTokenIsNotValid(refreshToken, externalSystemToken);

            var newAccessToken = GenerateAccessToken(externalSystem.Id, externalSystem.Name, SecurityRoles.ExternalSystem);
            // ReSharper disable once PossibleNullReferenceException
            externalSystemToken.AccessToken = newAccessToken;
            context.SaveChanges();

            return newAccessToken;
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

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        public void ThrowExceptionWhenTokenIsNotValid(string refreshToken, BaseToken token)
        {
            if (token == null)
                throw new SecurityTokenException("Invalid token.");

            if (token.RefreshToken != refreshToken)
                throw new SecurityTokenValidationException("Invalid token.");

            if (securityConfiguration.RefreshTokenCanExpire && token.ExpiresOn < DateTime.Now)
                throw new SecurityTokenExpiredException("Token expired.");
        }

        private Guid GetSidFromExpiredToken(string token)
		{
			var principal = GetPrincipalFromExpiredToken(token);
			var id = principal.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;
			if (Guid.TryParse(id, out Guid userId))
				return userId;
			return Guid.Empty;
		}

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
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

		private string GenerateAccessToken(Guid userId, string userName, string role)
        {
            var claims = new[]
			{
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
				new Claim(JwtRegisteredClaimNames.Sid, userId.ToString()),
			};

            return GenerateAccessToken(claims);
        }       

        private string GenerateAccessToken(Claim[] claims)
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

        private string GenerateRefreshToken()
		{
			var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

		public void EnsureAdminIsCreated()
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
					Password = new PasswordGenerator().EncodePassword("admincauca", securityConfiguration.PackageName)
				};
				context.Add(user);
				context.SaveChanges();
			}
		}

		public bool IsMobileVersionValid(string mobileVersion)
		{
			var mobile = new Version(mobileVersion);
			var minVersion = new Version(securityConfiguration.MinimalVersion);

			if (mobile.CompareTo(minVersion) < 0)
				return false;
			return true;
		}

        public List<AuthenticationUserPermission> GetActiveUserPermissions()
        {
            var idGroups = context.UserGroups
                .Where(ug => ug.IdUser == currentUserService.GetUserId())
                .Select(ug => ug.IdGroup);
            var restrictedPermissions = context.GroupPermissions
                .Where(g => idGroups.Contains(g.IdGroup) && g.IsAllowed == false)
                .Include(g => g.Permission)
                .Select(p => new AuthenticationUserPermission
                {
                    IdModulePermission = p.IdModulePermission,
                    Tag = p.Permission.Tag,
                    IsAllowed = p.IsAllowed,
                });
            var allowedPermissions = context.GroupPermissions
                .Where(g => idGroups.Contains(g.IdGroup) && g.IsAllowed && !restrictedPermissions
                                .Select(p => p.IdModulePermission).Contains(g.IdModulePermission))
                .Include(g => g.Permission)
                .Select(p => new AuthenticationUserPermission
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