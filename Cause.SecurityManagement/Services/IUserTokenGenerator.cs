using Cause.SecurityManagement.Models;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services;

public interface IUserTokenGenerator
{
    Task<UserToken> GenerateUserTokenAsync(User user, string role);
    UserToken GenerateUserToken(Guid userId, string accessToken, string refreshToken, string role, Guid? specificDeviceId = null);
}