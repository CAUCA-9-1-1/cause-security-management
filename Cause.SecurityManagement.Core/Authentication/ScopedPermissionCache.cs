using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Memoizes a user's merged permission set for the lifetime of one request.
/// Must be registered as scoped: a singleton would both create a captive dependency on the
/// scoped IUserPermissionService and defeat the design, because a revoked permission would
/// never become visible. A user's permissions cannot change mid-request, so no invalidation
/// is needed, and a permission revoked between requests is visible to the next request.
/// Not thread-safe by design; access is confined to this assembly and the authorization
/// pipeline invokes handlers sequentially.
/// </summary>
internal class ScopedPermissionCache(IUserPermissionService permissionService)
{
    private readonly Dictionary<Guid, List<UserMergedPermission>> permissionsByUser = [];

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)
    {
        var permissions = await GetPermissionsAsync(userId, cancellationToken);
        return permissions.Allows(permissionTag);
    }

    private async Task<List<UserMergedPermission>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (permissionsByUser.TryGetValue(userId, out var cached))
            return cached;

        var permissions = await permissionService.GetPermissionsForUserAsync(userId, cancellationToken);
        permissionsByUser[userId] = permissions;
        return permissions;
    }
}
