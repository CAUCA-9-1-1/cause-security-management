using System;

namespace Cause.SecurityManagement.Services
{
    public class InvalidTokenUserException(string token, string refreshToken, string userId) : Exception(
        $"UserId='{userId}' extracted from token='{token}' is unknown or invalid. RefreshToken='{refreshToken}'.");
}
