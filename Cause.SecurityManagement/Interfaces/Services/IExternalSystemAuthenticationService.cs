using System.Threading.Tasks;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Interfaces.Services
{
    public interface IExternalSystemAuthenticationService
    {
        (ExternalSystemToken token, ExternalSystem system) Login(string secretApiKey);
        Task<string> RefreshAccessTokenAsync(string token, string refreshToken);
    }
}