using System;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public class UserNameAvailabilityReader<TUser>(ISecurityContext<TUser> context) : IUserNameAvailabilityReader
        where TUser : User, new()
    {
        public async Task<bool> IsAvailableAsync(string userName, Guid userIdToIgnore, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            var taken = await context.Users
                .AnyAsync(user => user.IsActive
                    && user.UserName == userName
                    && user.Id != userIdToIgnore,
                    cancellationToken);

            return !taken;
        }
    }
}
