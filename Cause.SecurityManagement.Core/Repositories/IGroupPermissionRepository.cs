using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Core.Repositories
{
    public interface IGroupPermissionRepository
    {
        IQueryable<GroupPermission> GetForGroup(Guid groupId);
        IQueryable<GroupPermission> GetForUser(Guid userId);
        /// <summary>
        /// Returns the permission tags of every group the user belongs to, and whether each is allowed.
        /// The default implementation performs a blocking synchronous query; implementors
        /// should override it with a genuinely asynchronous one.
        /// </summary>
        Task<List<UserMergedPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(GetForUser(userId)
                .Select(groupPermission => new UserMergedPermission { Access = groupPermission.IsAllowed, FeatureName = groupPermission.Permission.Tag })
                .ToList());
        GroupPermission Get(Guid groupPermissinId);
        bool Any(Guid groupPermissionId);
        void Add(GroupPermission groupPermission);
        void Remove(GroupPermission groupPermission);
        void Update(GroupPermission groupPermission);
        void SaveChanges();
        Task SaveChangesAsync();
    }
}
