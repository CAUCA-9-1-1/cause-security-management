using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Core.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Core.Services;

public class UserTokenRefresher<TUser>(
    IUserRepository<TUser> userRepository,
    ITokenGenerator generator,
    ITokenReader tokenReader,
    IOptions<SecurityConfiguration> configuration,
    IDeviceManager deviceManager = null) 
        : BaseEntityTokenRefresher<TUser, UserToken>(userRepository, userRepository, generator, tokenReader, configuration, deviceManager), IUserTokenRefresher
    where TUser : User, new();