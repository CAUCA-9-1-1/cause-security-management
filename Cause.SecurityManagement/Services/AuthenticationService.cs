using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{

    public class AuthenticationService<TUser> 
        : IAuthenticationService
        where TUser : User, new()
    {
        private readonly ICurrentUserService currentUserService;
        private readonly IUserRepository<TUser> userRepository;
        private readonly IUserManagementService<TUser> userManagementService;
        private readonly IAuthenticationMultiFactorHandler<TUser> multiFactorHandler;
        private readonly ITokenReader tokenReader;
        private readonly ITokenGenerator generator;
        private readonly SecurityConfiguration configuration;

        public AuthenticationService(
            ICurrentUserService currentUserService,
            IUserRepository<TUser> userRepository,
            IUserManagementService<TUser> userManagementService,
            IAuthenticationMultiFactorHandler<TUser> multiFactorHandler,
            ITokenReader tokenReader,
            ITokenGenerator generator,
            IOptions<SecurityConfiguration> configuration)
        {
            this.currentUserService = currentUserService;
            this.userRepository = userRepository;
            this.userManagementService = userManagementService;
            this.multiFactorHandler = multiFactorHandler;
            this.tokenReader = tokenReader;
            this.generator = generator;
            this.configuration = configuration.Value;
        }

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
                (GenerateUserToken(userFound, role), userFound) :
                (null, null);
            await multiFactorHandler.SendValidationCodeWhenNeededAsync(user.userFound);            
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

        public virtual UserToken GenerateUserRecoveryToken(Guid userId)
        {
            var userFound = userRepository.GetUserById(userId);
            if (userFound != null)
            {
                return GenerateUserToken(userFound, SecurityRoles.UserRecovery);
            }
            return null;
        }

        public virtual UserToken GenerateUserCreationToken(Guid userId)
        {
            var accessToken = generator.GenerateAccessToken(userId, "temporary", SecurityRoles.UserCreation);
            return GenerateUserToken(userId, SecurityRoles.UserCreation, accessToken, "");
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
            return (userFound, MustValidateCode(userFound) ? SecurityRoles.UserLoginWithMultiFactor : GetSecurityRoleForUser(userFound));
        }        

        private static string GetSecurityRoleForUser(TUser userFound)
        {
            return userFound?.PasswordMustBeResetAfterLogin == true ? 
                SecurityRoles.UserPasswordSetup : 
                SecurityRoles.User;
        }

        public virtual bool MustValidateCode(User user)
        {
            return SecurityManagementOptions.MultiFactorAuthenticationIsActivated;
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

        public string RefreshUserToken(string token, string refreshToken)
        {
            var userId = tokenReader.GetSidFromExpiredToken(token);
            var userToken = userRepository.GetToken(userId, refreshToken);
            var user = userRepository.GetUserById(userId);
            if (user == null)
                throw new Exception($"User from id='{userId}' (from refreshToken='{refreshToken}') not found.");

            tokenReader.ThrowExceptionWhenTokenIsNotValid(refreshToken, userToken);

            var newAccessToken = generator.GenerateAccessToken(user.Id, user.UserName, SecurityRoles.User);
            // ReSharper disable once PossibleNullReferenceException
            userToken.AccessToken = newAccessToken;
            userRepository.SaveChanges();

            return newAccessToken;
        }

        public async Task SendNewCodeAsync()
        {
            var user = userRepository.GetUserById(currentUserService.GetUserId());
            await multiFactorHandler.SendNewValidationCodeAsync(user);
        }

        protected virtual UserToken GenerateUserToken(TUser user, string role)
        {
            var accessToken = generator.GenerateAccessToken(user.Id, user.UserName, role);
            var refreshToken = SecurityRoles.IsTemporaryRole(role) ? "" : generator.GenerateRefreshToken();
            var token = GenerateUserToken(user.Id, role, accessToken, refreshToken);
            userRepository.AddToken(token);
            return token;
        }

        private UserToken GenerateUserToken(Guid userId, string role, string accessToken, string refreshToken)
        {
            return new UserToken { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = generator.GenerateRefreshTokenExpirationDate(), IdUser = userId };
        }
    }
}