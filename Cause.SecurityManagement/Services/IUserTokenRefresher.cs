using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services;

public interface IUserTokenRefresher : IEntityTokenRefresher;

public interface IEntityTokenRefresher
{
    Task<string> GetNewAccessTokenAsync(string token, string refreshToken);
}