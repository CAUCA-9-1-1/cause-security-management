using Cause.SecurityManagement.Interfaces.Services;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Services
{
    public class ScopedDbContextProvider<TUser> : IScopedDbContextProvider<TUser>
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public ScopedDbContextProvider(ISecurityContext<TUser> context)
        {
            this.context = context;
        }

        public ISecurityContext<TUser> GetContext()
        {
            return context;
        }
    }
}
