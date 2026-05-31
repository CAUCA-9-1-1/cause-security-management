using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Core.Services
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
