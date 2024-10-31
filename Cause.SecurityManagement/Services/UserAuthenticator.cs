using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;
using System;

namespace Cause.SecurityManagement.Services;

public class UserAuthenticator<TUser>(
    ICurrentUserService currentUserService,
    IUserRepository<TUser> userRepository,
    IUserPermissionService userPermissions,
    IAuthenticationMultiFactorHandler<TUser> multiFactorHandler,
    ITokenGenerator generator,
    IUserTokenGenerator userTokenGenerator,
    IOptions<SecurityConfiguration> configuration)
    : BaseAuthenticator<TUser, UserToken> (currentUserService, userRepository, multiFactorHandler, userTokenGenerator, configuration), IUserAuthenticator
    where TUser : User, new()
{
    private readonly SecurityConfiguration configuration = configuration.Value;

    public virtual UserToken GenerateUserCreationToken(Guid userId)
    {
        var accessToken = generator.GenerateAccessToken(userId.ToString(), "temporary", SecurityRoles.UserCreation);
        return userTokenGenerator.GenerateEntityToken(userId, accessToken, "", SecurityRoles.UserCreation);
    }

    protected override bool HasRequiredPermissionToLogIn(TUser entity)
    {
        return string.IsNullOrWhiteSpace(configuration.RequiredPermissionForLogin)
               || userPermissions.HasPermission(entity.Id, configuration.RequiredPermissionForLogin);
    }
}