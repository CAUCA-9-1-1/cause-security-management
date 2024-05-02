using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services;

public class UserTokenRefresher<TUser>(
    IUserRepository<TUser> userRepository,
    ITokenGenerator generator,
    ITokenReader tokenReader,
    IOptions<SecurityConfiguration> configuration,
    IDeviceManager deviceManager = null) : IUserTokenRefresher
    where TUser : User, new()
{
    private readonly string tokenIssuer = configuration.Value.Issuer;

    public async Task<string> GetNewAccessTokenAsync(string token, string refreshToken)
    {
        ThrowExceptionWhenTokenHasNoValue(token, refreshToken);

        var userId = GetIdFromExpiredToken(token);
        var userToken = userRepository.GetToken(userId, refreshToken);
        var user = userRepository.GetUserById(userId);

        ThrowExceptionWhenWeCantGenerateNewToken(token, refreshToken, userId, user, userToken);
        await SetUserTokenDeviceIdWhenNotSetAsync(userToken);

        return await GenerateNewAccessToken(user, userToken);
    }

    private void ThrowExceptionWhenWeCantGenerateNewToken(string token, string refreshToken, Guid userId, TUser user, UserToken userToken)
    {
        ThrowExceptionIfUserHasNotBeenFound(token, refreshToken, userId, user);
        tokenReader.ThrowExceptionWhenTokenIsNotValid(refreshToken, userToken);
    }

    private async Task<string> GenerateNewAccessToken(TUser user, UserToken userToken)
    {
        var newAccessToken = generator.GenerateAccessToken(user.Id.ToString(), user.UserName, SecurityRoles.User, AdditionalClaimsGenerator.GetCustomClaims(userToken.SpecificDeviceId));
        // ReSharper disable once PossibleNullReferenceException
        userToken.AccessToken = newAccessToken;
        SetIssuerWhenNotAlreadySet(userToken);
        await userRepository.SaveChangesAsync();
        return newAccessToken;
    }

    private void SetIssuerWhenNotAlreadySet(UserToken userToken)
    {
        if (string.IsNullOrWhiteSpace(userToken.ForIssuer))
        {
            userToken.ForIssuer = tokenIssuer;
        }
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
        return Guid.TryParse(id, out var userId) ? userId : Guid.Empty;
    }

    private async Task SetUserTokenDeviceIdWhenNotSetAsync(UserToken token)
    {
        if (token.SpecificDeviceId != null || deviceManager == null)
            return;

        token.SpecificDeviceId = await deviceManager.GetCurrentDeviceId(token.IdUser);
    }

    private static void ThrowExceptionIfUserHasNotBeenFound(string token, string refreshToken, Guid userId, TUser user)
    {
        if (user == null)
            throw new InvalidTokenUserException(token, refreshToken, userId.ToString());
    }
}