using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Services
{
    public interface IExternalSystemAuthenticationService
    {
        (ExternalSystemToken token, ExternalSystem system) Login(string secretApiKey);
        string RefreshAccessToken(string token, string refreshToken);
    }
}