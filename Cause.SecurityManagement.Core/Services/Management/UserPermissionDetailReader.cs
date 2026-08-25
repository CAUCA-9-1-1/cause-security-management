using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public class UserPermissionDetailReader<TUser>(ISecurityContext<TUser> context)
        : IUserPermissionDetailReader
        where TUser : User, new()
    {
        public async Task<List<UserPermissionDetailDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var catalog = await ReadCatalogAsync(cancellationToken);
            var grants = await ReadGrantsAsync(userId, cancellationToken);
            var grantsByPermission = grants.GroupBy(grant => grant.IdModulePermission)
                .ToDictionary(group => group.Key, group => group.ToList());

            return catalog.ConvertAll(permission => BuildDetail(permission, grantsByPermission));
        }

        private Task<List<CatalogEntry>> ReadCatalogAsync(CancellationToken cancellationToken)
        {
            // Sequence alone is not a total order: catalog rows routinely share one, and the rows
            // within a shared value then come back in whatever order the provider chose, which
            // reads as random on screen and can differ between two calls.
            return context.ModulePermissions.AsNoTracking()
                .OrderBy(permission => permission.Sequence)
                .ThenBy(permission => permission.Name)
                .Select(permission => new CatalogEntry
                {
                    IdModulePermission = permission.Id,
                    Tag = permission.Tag,
                    Name = permission.Name,
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<PermissionGrant>> ReadGrantsAsync(Guid userId, CancellationToken cancellationToken)
        {
            var groupGrants = await context.GroupPermissions.AsNoTracking()
                .Where(groupPermission => context.UserGroups
                    .Any(userGroup => userGroup.IdUser == userId && userGroup.IdGroup == groupPermission.IdGroup))
                .Select(groupPermission => new PermissionGrant
                {
                    IdModulePermission = groupPermission.IdModulePermission,
                    Kind = UserPermissionOriginKind.Group,
                    GroupId = groupPermission.IdGroup,
                    GroupName = groupPermission.Group.Name,
                    IsAllowed = groupPermission.IsAllowed,
                })
                .ToListAsync(cancellationToken);

            var userGrants = await context.UserPermissions.AsNoTracking()
                .Where(userPermission => userPermission.IdUser == userId)
                .Select(userPermission => new PermissionGrant
                {
                    IdModulePermission = userPermission.IdModulePermission,
                    Kind = UserPermissionOriginKind.UserOverride,
                    IsAllowed = userPermission.IsAllowed,
                })
                .ToListAsync(cancellationToken);

            groupGrants.AddRange(userGrants);
            return groupGrants;
        }

        private static UserPermissionDetailDto BuildDetail(
            CatalogEntry permission,
            Dictionary<Guid, List<PermissionGrant>> grantsByPermission)
        {
            var grants = grantsByPermission.TryGetValue(permission.IdModulePermission, out var found)
                ? found
                : [];

            return new UserPermissionDetailDto
            {
                IdModulePermission = permission.IdModulePermission,
                Tag = permission.Tag,
                Name = permission.Name,
                Status = ComputeStatus(grants),
                Origins = grants.ConvertAll(grant => new UserPermissionOriginDto
                {
                    Kind = grant.Kind,
                    GroupId = grant.GroupId,
                    GroupName = grant.GroupName,
                    IsAllowed = grant.IsAllowed,
                }),
            };
        }

        /// <remarks>
        /// Mirrors <see cref="PermissionMergeTool"/>, which computes access as
        /// <c>group.All(p =&gt; p.Access)</c>. Reproducing that here rather than reporting the user
        /// override as authoritative is deliberate: a screen that disagreed with the engine deciding
        /// real access would be worse than one that shows an unexpected denial.
        /// </remarks>
        private static PermissionStatus ComputeStatus(List<PermissionGrant> grants)
        {
            if (grants.Count == 0)
            {
                return PermissionStatus.Undefined;
            }

            return grants.TrueForAll(grant => grant.IsAllowed)
                ? PermissionStatus.Allowed
                : PermissionStatus.Denied;
        }

        private sealed class CatalogEntry
        {
            public Guid IdModulePermission { get; set; }
            public string Tag { get; set; }
            public string Name { get; set; }
        }

        private sealed class PermissionGrant
        {
            public Guid IdModulePermission { get; set; }
            public UserPermissionOriginKind Kind { get; set; }
            public Guid? GroupId { get; set; }
            public string GroupName { get; set; }
            public bool IsAllowed { get; set; }
        }
    }
}
