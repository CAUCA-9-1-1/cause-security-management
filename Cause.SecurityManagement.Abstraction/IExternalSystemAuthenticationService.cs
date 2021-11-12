using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Services
{
    public interface IExternalSystemAuthenticationService
    {
        (ExternalSystemToken token, ExternalSystem system) LoginForExternalSystem(string secretApiKey);
        string RefreshExternalSystemToken(string token, string refreshToken);
    }
}