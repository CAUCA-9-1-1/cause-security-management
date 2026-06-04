using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;

namespace Cause.SecurityManagement.Core.Services.Management
{
    /// <summary>
    /// Backs the modernized group management UI (group edition, membership and user selection).
    /// The OData groups grid is fed by a consumer-provided queryable, not by this service.
    /// Every operation honors the supplied <see cref="CancellationToken"/> so a cancelled request
    /// short-circuits before reaching the database.
    /// </summary>
    public interface IGroupManagementApiService
    {
        /// <summary>Returns the full group payload, or <c>null</c> when the group does not exist.</summary>
        Task<GroupDto> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

        /// <summary>Inserts or updates a group, its permission overrides and its membership.</summary>
        Task<GroupDto> SaveGroupAsync(GroupDto group, CancellationToken cancellationToken = default);

        /// <summary>Deletes a group and its dependents. Returns <c>false</c> when not found.</summary>
        Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

        /// <summary>Returns the users that are members of the group.</summary>
        Task<List<UserForGroupDto>> GetGroupUsersAsync(Guid groupId, CancellationToken cancellationToken = default);

        /// <summary>Server-side paged search over all active users.</summary>
        Task<UserSearchResultDto> SearchUsersAsync(UserSearchRequestDto request, CancellationToken cancellationToken = default);
    }
}
