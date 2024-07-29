using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using System;

namespace Cause.SecurityManagement.Services;

public abstract class BaseEntityTokenRefresher<TEntity, TEntityToken>(
    IEntityTokenRepository<TEntityToken> tokenRepository,
    IAuthenticableEntityRepository<TEntity> entityRepository,
    ITokenGenerator generator,
    ITokenReader tokenReader,
    IOptions<SecurityConfiguration> configuration,
    IDeviceManager deviceManager = null
    ) : IEntityTokenRefresher
    where TEntity : IAuthenticableEntity
    where TEntityToken : BaseToken
{
    private readonly string tokenIssuer = configuration.Value.Issuer;

    public async Task<string> GetNewAccessTokenAsync(string token, string refreshToken)
    {
        ThrowExceptionWhenTokenHasNoValue(token, refreshToken);

        var entityId = GetIdFromExpiredToken(token);
        var entityToken = tokenRepository.GetToken(entityId, refreshToken);
        var entity = entityRepository.GetEntityById(entityId);

        ThrowExceptionWhenWeCantGenerateNewToken(token, refreshToken, entityId, entity, entityToken);
        await SetEntityTokenDeviceIdWhenNotSetAsync(entityToken, entityId);

        return await GenerateNewAccessToken(entity, entityToken);
    }

    private void ThrowExceptionWhenWeCantGenerateNewToken(string token, string refreshToken, Guid userId, TEntity entity, TEntityToken entityToken)
    {
        ThrowExceptionIfUserHasNotBeenFound(token, refreshToken, userId, entity);
        tokenReader.ThrowExceptionWhenTokenIsNotValid(refreshToken, entityToken);
    }

    private async Task<string> GenerateNewAccessToken(TEntity entity, TEntityToken entityToken)
    {
        var newAccessToken = generator.GenerateAccessToken(entity.Id.ToString(), entity.UserName, SecurityRoles.User, AdditionalClaimsGenerator.GetCustomClaims(entityToken.SpecificDeviceId));
        // ReSharper disable once PossibleNullReferenceException
        entityToken.AccessToken = newAccessToken;
        SetIssuerWhenNotAlreadySet(entityToken);
        await tokenRepository.SaveChangesAsync();
        return newAccessToken;
    }

    private void SetIssuerWhenNotAlreadySet(TEntityToken userToken)
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

    private async Task SetEntityTokenDeviceIdWhenNotSetAsync(TEntityToken token, Guid entityId)
    {
        if (token.SpecificDeviceId != null || deviceManager == null)
            return;

        token.SpecificDeviceId = await deviceManager.GetCurrentDeviceIdAsync(entityId);
    }

    private static void ThrowExceptionIfUserHasNotBeenFound(string token, string refreshToken, Guid userId, TEntity user)
    {
        if (user == null)
            throw new InvalidTokenUserException(token, refreshToken, userId.ToString());
    }
}