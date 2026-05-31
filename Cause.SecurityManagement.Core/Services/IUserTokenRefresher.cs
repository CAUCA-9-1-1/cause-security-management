using System.Threading.Tasks;

namespace Cause.SecurityManagement.Core.Services;

public interface IUserTokenRefresher : IEntityTokenRefresher;

public interface IEntityTokenRefresher
{
    Task<string> GetNewAccessTokenAsync(string token, string refreshToken);
}