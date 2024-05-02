using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services;

public interface IUserTokenRefresher
{
    Task<string> GetNewAccessTokenAsync(string token, string refreshToken);
}