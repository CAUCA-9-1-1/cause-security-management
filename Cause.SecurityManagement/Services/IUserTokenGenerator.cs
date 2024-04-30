using Cause.SecurityManagement.Models;
using System;

namespace Cause.SecurityManagement.Services;

public interface IUserTokenGenerator
{
    UserToken GenerateUserToken(User user, string role);
    UserToken GenerateUserToken(Guid userId, string accessToken, string refreshToken, string role);
}