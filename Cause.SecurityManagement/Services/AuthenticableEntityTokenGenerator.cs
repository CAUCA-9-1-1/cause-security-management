using System;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services;

public abstract class AuthenticableEntityTokenGenerator<T>(
    IOptions<SecurityConfiguration> configuration,
    ITokenGenerator generator,
    IEntityTokenRepository<T> repository,
    IDeviceManager deviceManager = null) where T : BaseToken
{
    public virtual async Task<T> GenerateEntityTokenAsync(IAuthenticableEntity entity, string role)
    {
        var specificDeviceId = await GenerateDeviceWhenNecessaryIdAsync(entity.Id, SecurityRoles.IsTemporaryRole(role));
        var accessToken = generator.GenerateAccessToken(entity.Id.ToString(), entity.UserName, role, AdditionalClaimsGenerator.GetCustomClaims(specificDeviceId));
        var refreshToken = SecurityRoles.IsTemporaryRole(role) ? "" : generator.GenerateRefreshToken();
        await RemoveExistingTokenWhenNeededAsync(entity, role);
        var token = GenerateEntityToken(entity.Id, accessToken, refreshToken, role, specificDeviceId);
        repository.AddToken(token);
        return token;
    }

    private async Task RemoveExistingTokenWhenNeededAsync(IAuthenticableEntity entity, string role)
    {
        if (MustRemoveExistingToken(role))
        {
            await repository.RemoveExistingTokenAsync(entity.Id, configuration.Value.Issuer);
        }
    }

    private bool MustRemoveExistingToken(string role)
    {
        return MustGenerateNewDevice(SecurityRoles.IsTemporaryRole(role));
    }

    public T GenerateEntityToken(Guid entityId, string accessToken, string refreshToken, string role, Guid? specificDeviceId = null)
    {
        var token = GenerateNewEntityToken(entityId);
        token.AccessToken = accessToken;
        token.RefreshToken = refreshToken;
        token.ExpiresOn = generator.GenerateRefreshTokenExpirationDate();
        token.ForIssuer = configuration.Value.Issuer;
        token.Role = role;
        token.SpecificDeviceId = specificDeviceId;
        return token;
    }

    protected abstract T GenerateNewEntityToken(Guid entityId);

    private async Task<Guid?> GenerateDeviceWhenNecessaryIdAsync(Guid entityId, bool isTemporaryRole)
    {
        return MustGenerateNewDevice(isTemporaryRole) ? await deviceManager.CreateNewDeviceAsync(entityId) : null;
    }

    private bool MustGenerateNewDevice(bool isTemporaryRole)
    {
        return !(isTemporaryRole || deviceManager == null);
    }
}