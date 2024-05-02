using System;

namespace Cause.SecurityManagement.Services
{
    public interface ITokenGenerator
    {
        string GenerateAccessToken(string entityId, string entityName, string role, params CustomClaims[] additionalClaims);
        string GenerateRefreshToken();
        int GetRefreshTokenLifeTimeInMinute();
        DateTime GenerateRefreshTokenExpirationDate();
    }
}
