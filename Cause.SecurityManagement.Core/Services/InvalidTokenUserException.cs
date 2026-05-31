using System;

namespace Cause.SecurityManagement.Core.Services;

public class InvalidTokenUserException(string token, string refreshToken, string userId) : Exception(
    $"UserId='{userId}' extracted from token='{token}' is unknown, invalid or not allowed. RefreshToken='{refreshToken}'.");