using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public class UserAuthenticator<TUser>(
        ICurrentUserService currentUserService,
        IUserRepository<TUser> userRepository,
        IUserManagementService<TUser> userManagementService,
        IAuthenticationMultiFactorHandler<TUser> multiFactorHandler,
        ITokenGenerator generator,
        IUserTokenGenerator userTokenGenerator,
        IOptions<SecurityConfiguration> configuration)
        : IUserAuthenticator
        where TUser : User, new()
    {
        private readonly SecurityConfiguration configuration = configuration.Value;

        public virtual async Task<(UserToken token, User user)> LoginAsync(string userName, string password)
        {
            var (userFound, roles) = GetUserWithTemporaryPassword(userName, password)
                ?? GetUser(userName, password);
            return await GenerateTokenIfUserCanLogInAsync(userFound, roles);
        }

        protected virtual (TUser user, string role)? GetUserWithTemporaryPassword(string userName, string password)
        {
            var tempUser = userRepository.GetUserWithTemporaryPassword(userName, password);
            if (tempUser != null && CanLogIn(tempUser))
            {
                return (tempUser, SecurityRoles.UserPasswordSetup);
            }
            return null;
        }

        protected virtual async Task<(UserToken token, TUser user)> GenerateTokenIfUserCanLogInAsync(TUser userFound, string role)
        {
            var user = CanLogIn(userFound) ?
                (await userTokenGenerator.GenerateUserTokenAsync(userFound, role), userFound) :
                (null, null);
            await multiFactorHandler.SendValidationCodeWhenNeededAsync(user.userFound);
            return user;
        }

        public async Task<(UserToken token, User user)> ValidateMultiFactorCodeAsync(ValidationInformation validationInformation)
        {
            var (userFound, roles) = GetUser(currentUserService.GetUserId());
            if (userFound != null && await multiFactorHandler.CodeIsValidAsync(userFound, validationInformation.ValidationCode, ValidationCodeType.MultiFactorLogin))
            {
                return (await userTokenGenerator.GenerateUserTokenAsync(userFound, roles), userFound);
            }
            throw new InvalidValidationCodeException($"Validation code {validationInformation.ValidationCode} is invalid for this user.");
        }

        public virtual async Task<UserToken> GenerateUserRecoveryTokenAsync(Guid userId)
        {
            var userFound = userRepository.GetUserById(userId);
            if (userFound != null)
            {
                return await userTokenGenerator.GenerateUserTokenAsync(userFound, SecurityRoles.UserRecovery);
            }
            return null;
        }

        public virtual UserToken GenerateUserCreationToken(Guid userId)
        {
            var accessToken = generator.GenerateAccessToken(userId.ToString(), "temporary", SecurityRoles.UserCreation);
            return userTokenGenerator.GenerateUserToken(userId, accessToken, "", SecurityRoles.UserCreation);
        }

        protected virtual (TUser user, string rolesToGive) GetUser(Guid idUser)
        {
            var userFound = userRepository.GetUserById(idUser);
            return (userFound, GetSecurityRoleForUser(userFound));
        }

        protected virtual (TUser user, string rolesToGive) GetUser(string userName, string password)
        {
            var encodedPassword = new PasswordGenerator().EncodePassword(password, configuration.PackageName);
            var userFound = userRepository.GetUser(userName, encodedPassword);
            if (userFound == null)
                return (null, null);
            return (userFound, GetRoleFromUser(userFound));
        }

        private string GetRoleFromUser(TUser userFound)
        {
            if (userFound.PasswordMustBeResetAfterLogin)
                return SecurityRoles.UserPasswordSetup;

            return MustValidateCode(userFound)
                ? SecurityRoles.UserLoginWithMultiFactor
                : GetSecurityRoleForUser(userFound);
        }

        private static string GetSecurityRoleForUser(TUser userFound)
        {
            return userFound.PasswordMustBeResetAfterLogin ?
                SecurityRoles.UserPasswordSetup :
                SecurityRoles.User;
        }

        public virtual bool MustValidateCode(User user)
        {
            return SecurityManagementOptions.MultiFactorAuthenticationIsActivated && user.TwoFactorAuthenticatorEnabled;
        }

        public bool IsLoggedIn(string refreshToken)
        {
            return userRepository.HasToken(currentUserService.GetUserId(), refreshToken);
        }

        protected virtual bool CanLogIn(User user)
        {
            return user != null && HasRequiredPermissionToLogIn(user);
        }

        protected virtual bool HasRequiredPermissionToLogIn(User user)
        {
            return string.IsNullOrWhiteSpace(configuration.RequiredPermissionForLogin)
                || userManagementService.HasPermission(user.Id, configuration.RequiredPermissionForLogin);
        }

        public async Task SendNewCodeAsync()
        {
            var user = userRepository.GetUserById(currentUserService.GetUserId());
            await multiFactorHandler.SendNewValidationCodeAsync(user);
        }
    }
}