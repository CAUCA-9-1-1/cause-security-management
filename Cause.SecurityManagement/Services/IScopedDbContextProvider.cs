using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Services
{
    public interface IScopedDbContextProvider<TUser>
        where TUser : User, new()
    {
        ISecurityContext<TUser> GetContext();
    }
}
