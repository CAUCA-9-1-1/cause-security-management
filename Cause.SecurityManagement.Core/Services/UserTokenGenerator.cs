using System;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Core.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Core.Services;

public class UserTokenGenerator<TUser>(
    IOptions<SecurityConfiguration> configuration,
    ITokenGenerator generator,
    IUserRepository<TUser> repository,
    IDeviceManager deviceManager = null) : AuthenticableEntityTokenGenerator<UserToken> (configuration, generator, repository, deviceManager), IUserTokenGenerator
    where TUser : User, new()
{
    protected override UserToken GenerateNewEntityToken(Guid entityId)
    {
        return new UserToken { IdUser = entityId };
    }

    public Task<UserToken> GenerateUserTokenAsync(User user, string role)
    {
        return GenerateEntityTokenAsync(user, role);
    }

    public UserToken GenerateUserToken(Guid userId, string accessToken, string refreshToken, string role, Guid? specificDeviceId = null)
    {
        return GenerateEntityToken(userId, accessToken, refreshToken, role, specificDeviceId);
    }
}