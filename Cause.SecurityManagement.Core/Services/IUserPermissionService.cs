using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Core.Services
{
    public interface IUserPermissionService
    {
        bool HasPermission(Guid userId, string permissionTag);
        List<UserMergedPermission> GetPermissionsForUser(Guid userId);

        /// <summary>
        /// Returns whether the user holds the named permission.
        /// The default implementation delegates to the synchronous member and therefore blocks;
        /// the shipped UserPermissionService overrides it. A consumer-supplied implementation
        /// relying on this default will block a request thread during authorization.
        /// </summary>
        Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)
            => Task.FromResult(HasPermission(userId, permissionTag));

        /// <summary>
        /// Returns the user's merged permission set.
        /// The default implementation delegates to the synchronous member and therefore blocks;
        /// the shipped UserPermissionService overrides it. A consumer-supplied implementation
        /// relying on this default will block a request thread during authorization.
        /// </summary>
        Task<List<UserMergedPermission>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(GetPermissionsForUser(userId));
    }
}
