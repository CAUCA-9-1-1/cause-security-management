using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Services
{
    public class ScopedDbContextProvider<TUser>(ISecurityContext<TUser> context) : IScopedDbContextProvider<TUser>
        where TUser : User, new()
    {
        public ISecurityContext<TUser> GetContext()
        {
            return context;
        }
    }
}
