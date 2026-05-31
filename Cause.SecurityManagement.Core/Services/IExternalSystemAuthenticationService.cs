using System.Threading.Tasks;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Core.Services
{
    public interface IExternalSystemAuthenticationService
    {
        (ExternalSystemToken token, ExternalSystem system) Login(string secretApiKey);
        Task<string> RefreshAccessTokenAsync(string token, string refreshToken);
    }
}