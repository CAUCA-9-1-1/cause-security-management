using System;

namespace Cause.SecurityManagement.Services
{
    public class InvalidTokenUserException : Exception
    ­{
        public InvalidTokenUserException (string token, string refreshToken, string userId)
            : base($"UserId='{userId}' extracted from token='{token}' is unknown or invalid. RefreshToken='{refreshToken}'.")
        {
        }
    }
}
