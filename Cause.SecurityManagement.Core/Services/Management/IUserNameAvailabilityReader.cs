using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Core.Services.Management
{
    /// <summary>
    /// Answers whether a user name may be claimed. Deactivated users do not hold their name.
    /// </summary>
    public interface IUserNameAvailabilityReader
    {
        /// <summary>Whether <paramref name="userName"/> may be claimed.</summary>
        /// <param name="userName">The name being claimed.</param>
        /// <param name="userIdToIgnore">
        /// The user being edited. Passing it is what stops an unchanged user from reporting its own
        /// name as taken; pass <see cref="Guid.Empty"/> when creating.
        /// </param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <returns>
        /// <c>true</c> when no other active user holds <paramref name="userName"/> — i.e. the name is
        /// available, not that it is taken. A null, empty or whitespace-only <paramref name="userName"/>
        /// is never available.
        /// </returns>
        Task<bool> IsAvailableAsync(string userName, Guid userIdToIgnore, CancellationToken cancellationToken = default);
    }
}
