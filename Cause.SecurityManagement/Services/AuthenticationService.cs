using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Cause.SecurityManagement.Services
{

    public class AuthenticationService<TUser> 
        : BaseAuthenticationService<TUser>, IAuthenticationService
        where TUser : User, new()
    {
        private readonly ICurrentUserService currentUserService;        
        private readonly IUserManagementService<TUser> userManagementService;
        private readonly IAuthenticationMultiFactorHandler<TUser> multiFactorHandler;

        public AuthenticationService(
            ICurrentUserService currentUserService,
            ISecurityContext<TUser> context,
            IUserManagementService<TUser> userManagementService,
            IOptions<SecurityConfiguration> securityOptions,
            IAuthenticationMultiFactorHandler<TUser> multiFactorHandler)
            : base(context, securityOptions)
        {
            this.currentUserService = currentUserService;
            this.userManagementService = userManagementService;
            this.multiFactorHandler = multiFactorHandler;
        }

        public virtual (UserToken token, User user) Login(string userName, string password)
        {
            var (userFound, roles) = GetUserWithTemporaryPassword(userName, password)
                ?? GetUser(userName, password);
            return GenerateUserIfUserCanLogIn(userFound, roles);
        }

        protected virtual (TUser user, string role)? GetUserWithTemporaryPassword(string userName, string password)
        {
            var tempUser = TryToGetUserWithTemporaryPassword(userName, password);
            if (tempUser != null && CanLogIn(tempUser))
            {
                return (tempUser, SecurityRoles.UserPasswordSetup);
            }
            return null;
        }

        protected virtual (UserToken token, TUser user) GenerateUserIfUserCanLogIn(TUser userFound, string roles)
        {
            var user = CanLogIn(userFound) ?
                (GenerateUserToken(userFound, roles), userFound) :
                (null, null);
            multiFactorHandler.SendValidationCodeWhenNeeded(user.userFound);            
            return user;
        }     

        public (UserToken token, User user) ValidateMultiFactorCode(ValidationInformation validationInformation)
        {
            var (userFound, roles) = GetUser(currentUserService.GetUserId());
            if (userFound != null && multiFactorHandler.CodeIsValid(userFound.Id, validationInformation.ValidationCode, ValidationCodeType.MultiFactorLogin))
            {
                return (GenerateUserToken(userFound, roles), userFound);
            }
            throw new InvalidValidationCodeException($"Validation code {validationInformation.ValidationCode} is invalid for this user.");
        }        

        protected virtual UserToken GenerateUserToken(TUser user, string roles)
        {
            var tokenLifeTimeInMinute = GetRefreshTokenLifeTimeInMinute();
            return GenerateUserToken(user, roles, tokenLifeTimeInMinute);
        }

        protected virtual UserToken GenerateUserToken(TUser user, string roles, int tokenLifeTimeInMinute, bool setRefreshToken = true)
        {
            var accessToken = GenerateAccessToken(user.Id, user.UserName, roles, GetAccessTokenLifeTimeInMinute());
            var refreshToken = setRefreshToken ? GenerateRefreshToken() : "";
            var token = new UserToken { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = DateTime.Now.AddMinutes(tokenLifeTimeInMinute), IdUser = user.Id };
            context.Add(token);
            context.SaveChanges();
            return token;
        }

        protected virtual (TUser user, string rolesToGive) GetUser(Guid idUser)
        {
            var userFound = context.Users
                .SingleOrDefault(user => user.Id == idUser && user.IsActive);
            return (userFound, GetSecurityRoleForUser(userFound));
        }

        protected virtual (TUser user, string rolesToGive) GetUser(string userName, string password)
        {
            var encodedPassword = new PasswordGenerator().EncodePassword(password, securityConfiguration.PackageName);
            var userFound = context.Users
                .SingleOrDefault(user => user.UserName == userName && user.Password.ToUpper() == encodedPassword && user.IsActive);
            return (userFound, SecurityManagementOptions.MultiFactorAuthenticationIsActivated ? SecurityRoles.UserLoginWithMultiFactor : GetSecurityRoleForUser(userFound));
        }

        protected TUser TryToGetUserWithTemporaryPassword(string userName, string password)
        {
            return context.Users.FirstOrDefault(user => user.UserName == userName && user.Password == password && user.PasswordMustBeResetAfterLogin && user.IsActive);
        }

        private static string GetSecurityRoleForUser(TUser userFound)
        {
            return userFound?.PasswordMustBeResetAfterLogin == true ? 
                SecurityRoles.UserPasswordSetup : 
                SecurityRoles.User;
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
            var accessToken = GenerateAccessToken(userId, "temporary", SecurityRoles.UserCreation, GetTemporaryAccessTokenLifeTimeInMinute());
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

            var newAccessToken = GenerateAccessToken(user.Id, user.UserName, SecurityRoles.User, GetAccessTokenLifeTimeInMinute());
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
    }
}