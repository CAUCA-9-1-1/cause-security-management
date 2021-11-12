using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cause.SecurityManagement.Services
{
    public class AuthenticationService<TUser> 
        : BaseAuthenticationService<TUser>, IAuthenticationService
        where TUser : User, new()
    {
        private readonly ICurrentUserService currentUserService;        
        private readonly IUserManagementService<TUser> userManagementService;

        public AuthenticationService(
            ICurrentUserService currentUserService,
            ISecurityContext<TUser> context, 
            IUserManagementService<TUser> userManagementService,
            IOptions<SecurityConfiguration> securityOptions) : base(context, securityOptions)
		{
            this.currentUserService = currentUserService;
            this.userManagementService = userManagementService;
        }

		public virtual (UserToken token, User user) Login(string userName, string password)
        {
            var (userFound, roles) = GetUser(userName, password);
            return CanLogIn(userFound) ?
                (GenerateUserToken(userFound, roles), userFound) :
                (null, null);
        }

        protected virtual UserToken GenerateUserToken(TUser user, string roles)
        {
            var tokenLifeTimeInMinute = GetRefreshTokenLifeTimeInMinute();
            return GenerateUserToken(user, roles, tokenLifeTimeInMinute);
        }

        protected virtual UserToken GenerateUserToken(TUser user, string roles, int tokenLifeTimeInMinute, bool setRefreshToken = true)
        {
            var accessToken = GenerateAccessToken(user.Id, user.UserName, roles);
            var refreshToken = setRefreshToken ? GenerateRefreshToken() : "";
            var token = new UserToken { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = DateTime.Now.AddMinutes(tokenLifeTimeInMinute), IdUser = user.Id };
            context.Add(token);
            context.SaveChanges();
            return token;
        }

        protected virtual (TUser user, string rolesToGive) GetUser(string userName, string password)
        {
            var encodedPassword = new PasswordGenerator().EncodePassword(password, securityConfiguration.PackageName);
            var userFound = context.Users
                .SingleOrDefault(user => user.UserName == userName && user.Password.ToUpper() == encodedPassword && user.IsActive);
            return (userFound, SecurityRoles.User);
        }

        protected virtual bool CanLogIn(User user)
        {
            return user != null && HasRequiredPermissionToLogIn(user);
        }

        protected virtual bool HasRequiredPermissionToLogIn(User user)
        {
            return string.IsNullOrWhiteSpace(securityConfiguration.RequiredPermissionForLogin)
                || userManagementService.HasPermission(user.Id, securityConfiguration.RequiredPermissionForLogin);
        }

        public virtual UserToken GenerateUserRecoveryToken(Guid userId)
        {
            var userFound = context.Users
                .SingleOrDefault(user => user.Id == userId && user.IsActive);
            if (userFound != null)
            {
                return GenerateUserToken(userFound, SecurityRoles.UserRecovery, GetTemporaryAccessTokenLifeTimeInMinute(), false);
            }
            return null;
        }

        public virtual UserToken GenerateUserCreationToken(Guid userId)
        {            
            var accessToken = GenerateAccessToken(userId, "temporary", SecurityRoles.UserCreation);
            var token = new UserToken { AccessToken = accessToken, RefreshToken = "", ExpiresOn = DateTime.Now.AddMinutes(GetTemporaryAccessTokenLifeTimeInMinute()), IdUser = userId };
            return token;
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

        public bool IsMobileVersionLatest(string mobileVersion)
        {
            var mobile = new Version(mobileVersion);
            var latestVersion = new Version(securityConfiguration.LatestVersion);

            if (mobile.CompareTo(latestVersion) < 0)
                return false;
            return true;
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