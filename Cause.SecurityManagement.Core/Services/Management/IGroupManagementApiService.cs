using System;
using System.Collections.Generic;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;

namespace Cause.SecurityManagement.Core.Services.Management
{
    /// <summary>
    /// Backs the modernized group management UI (group edition, membership and user selection).
    /// The OData groups grid is fed by a consumer-provided queryable, not by this service.
    /// </summary>
    public interface IGroupManagementApiService
    {
        /// <summary>Returns the full group payload, or <c>null</c> when the group does not exist.</summary>
        GroupDto GetGroup(Guid groupId);

        /// <summary>Inserts or updates a group, its permission overrides and its membership.</summary>
        GroupDto SaveGroup(GroupDto group);

        /// <summary>Deletes a group and its dependents. Returns <c>false</c> when not found.</summary>
        bool DeleteGroup(Guid groupId);

        /// <summary>Returns the users that are members of the group.</summary>
        List<UserForGroupDto> GetGroupUsers(Guid groupId);

        /// <summary>Server-side paged search over all active users.</summary>
        UserSearchResultDto SearchUsers(UserSearchRequestDto request);
    }
}
