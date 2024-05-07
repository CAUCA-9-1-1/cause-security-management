﻿using System;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services;

public class UserTokenGenerator<TUser>(
    IOptions<SecurityConfiguration> configuration,
    ITokenGenerator generator,
    IUserRepository<TUser> repository,
    IDeviceManager deviceManager = null) : IUserTokenGenerator
    where TUser : User, new()
{
    public virtual async Task<UserToken> GenerateUserTokenAsync(User user, string role)
    {
        var specificDeviceId = await GenerateDeviceWhenNecessaryIdAsync(user.Id, SecurityRoles.IsTemporaryRole(role));
        var accessToken = generator.GenerateAccessToken(user.Id.ToString(), user.UserName, role, AdditionalClaimsGenerator.GetCustomClaims(specificDeviceId));
        var refreshToken = SecurityRoles.IsTemporaryRole(role) ? "" : generator.GenerateRefreshToken();
        await RemoveExistingTokenWhenNeededAsync(user, role);
        var token = GenerateUserToken(user.Id, accessToken, refreshToken, role, specificDeviceId);
        repository.AddToken(token);
        return token;
    }

    private async Task RemoveExistingTokenWhenNeededAsync(User user, string role)
    {
        if (MustRemoveExistingToken(role))
        {
            await repository.RemoveExistingTokenAsync(user.Id, configuration.Value.Issuer);
        }
    }

    private bool MustRemoveExistingToken(string role)
    {
        return MustGenerateNewDevice(SecurityRoles.IsTemporaryRole(role));
    }

    public UserToken GenerateUserToken(Guid userId, string accessToken, string refreshToken, string role, Guid? specificDeviceId = null)
    {
        return new UserToken
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresOn = generator.GenerateRefreshTokenExpirationDate(),
            ForIssuer = configuration.Value.Issuer,
            Role = role,
            IdUser = userId,
            SpecificDeviceId = specificDeviceId
        };
    }

    private async Task<Guid?> GenerateDeviceWhenNecessaryIdAsync(Guid userId, bool isTemporaryRole)
    {
        return MustGenerateNewDevice(isTemporaryRole) ? await deviceManager.CreateNewDeviceAsync(userId) : null;
    }

    private bool MustGenerateNewDevice(bool isTemporaryRole)
    {
        return !(isTemporaryRole || deviceManager == null);
    }
}