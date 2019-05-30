using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthentificationService
    {
        void EnsureAdminIsCreated(string applicationName);
        bool IsMobileVersionValid(string mobileVersion, string minimalVersion);
        (UserToken token, User user) Login(string userName, string password, string applicationName, string issuer, string secretKey);
        string Refresh(string token, string refreshToken, string applicationName, string issuer, string secretKey);
    }
}