﻿using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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

        public async Task<(UserToken token, User user)> ValidateMultiFactorCodeAsync(ValidationInformation validationInformation)
        {
            var (userFound, roles) = GetUser(currentUserService.GetUserId());
            if (userFound != null && await multiFactorHandler.CodeIsValidAsync(userFound, validationInformation.ValidationCode, ValidationCodeType.MultiFactorLogin))
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
            var accessToken = generator.GenerateAccessToken(userId.ToString(), "temporary", SecurityRoles.UserCreation);
            return GenerateUserToken(userId, accessToken, "");
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

        protected virtual bool CanLogIn(User user)
        {
            return user != null && HasRequiredPermissionToLogIn(user);
        }

        protected virtual bool HasRequiredPermissionToLogIn(User user)
        {
            return string.IsNullOrWhiteSpace(configuration.RequiredPermissionForLogin)
                || userManagementService.HasPermission(user.Id, configuration.RequiredPermissionForLogin);
        }

        public async Task<string> RefreshUserTokenAsync(string token, string refreshToken)
        {
            ThrowExceptionWhenTokenHasNoValue(token, refreshToken);

            var userId = GetIdFromExpiredToken(token);
            var userToken = userRepository.GetToken(userId, refreshToken);
            var user = userRepository.GetUserById(userId);

            ThrowExceptionIfUserHasNotBeenFound(token, refreshToken, userId, user);
            tokenReader.ThrowExceptionWhenTokenIsNotValid(refreshToken, userToken);

            var newAccessToken = generator.GenerateAccessToken(user.Id.ToString(), user.UserName, SecurityRoles.User);
            // ReSharper disable once PossibleNullReferenceException
            userToken.AccessToken = newAccessToken;
            await userRepository.SaveChangesAsync();

            return newAccessToken;
        }

        private static void ThrowExceptionWhenTokenHasNoValue(string token, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new SecurityTokenException("Invalid token.");
            }
        }

        private Guid GetIdFromExpiredToken(string token)
        {
            var id = tokenReader.GetSidFromExpiredToken(token);
            if (Guid.TryParse(id, out Guid userId))
                return userId;
            return Guid.Empty;
        }

        private static void ThrowExceptionIfUserHasNotBeenFound(string token, string refreshToken, Guid userId, TUser user)
        {
            if (user == null)
                throw new InvalidTokenUserException(token, refreshToken, userId.ToString());
        }

        public async Task SendNewCodeAsync()
        {
            var user = userRepository.GetUserById(currentUserService.GetUserId());
            await multiFactorHandler.SendNewValidationCodeAsync(user);
        }

        protected virtual UserToken GenerateUserToken(TUser user, string role)
        {
            var accessToken = generator.GenerateAccessToken(user.Id.ToString(), user.UserName, role);
            var refreshToken = SecurityRoles.IsTemporaryRole(role) ? "" : generator.GenerateRefreshToken();
            var token = GenerateUserToken(user.Id, accessToken, refreshToken);
            userRepository.AddToken(token);
            return token;
        }

        private UserToken GenerateUserToken(Guid userId, string accessToken, string refreshToken)
        {
            return new UserToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresOn = generator.GenerateRefreshTokenExpirationDate(),
                ForIssuer = configuration.Issuer,
                IdUser = userId
            };
        }
    }
}