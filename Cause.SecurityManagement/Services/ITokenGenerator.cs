using System;

namespace Cause.SecurityManagement.Services
{
    public interface ITokenGenerator
    {
        string GenerateAccessToken(Guid entityId, string entityName, string role);
        string GenerateRefreshToken();
        int GetRefreshTokenLifeTimeInMinute();
        DateTime GenerateAccessExpirationDateByRole(string role);
    }
}
