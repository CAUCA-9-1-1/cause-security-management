using System;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services;

public class UserTokenGenerator(
    IOptions<SecurityConfiguration> configuration,
    ITokenGenerator generator,
    IUserRepository<User> repository,
    IDeviceManager deviceManager = null) : IUserTokenGenerator
{
    public virtual UserToken GenerateUserToken(User user, string role)
    {
        var accessToken = generator.GenerateAccessToken(user.Id.ToString(), user.UserName, role);
        var refreshToken = SecurityRoles.IsTemporaryRole(role) ? "" : generator.GenerateRefreshToken();
        var token = GenerateUserToken(user.Id, accessToken, refreshToken, role);
        repository.AddToken(token);
        return token;
    }

    public UserToken GenerateUserToken(Guid userId, string accessToken, string refreshToken, string role)
    {
        return new UserToken
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresOn = generator.GenerateRefreshTokenExpirationDate(),
            ForIssuer = configuration.Value.Issuer,
            Role = role,
            IdUser = userId,
            SpecificDeviceId = GenerateDeviceWhenNecessaryId(userId, SecurityRoles.IsTemporaryRole(role)),
        };
    }

    private Guid? GenerateDeviceWhenNecessaryId(Guid userId, bool isTemporaryRole)
    {
        return isTemporaryRole? null : deviceManager?.CreateNewDevice(userId);
    }
}