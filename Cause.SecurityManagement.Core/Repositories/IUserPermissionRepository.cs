using System;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Core.Repositories
{
    public interface IUserPermissionRepository
    {
        Task<List<AuthenticationUserPermission>> GetUserPermissionsAsync(Guid userId);
        List<UserPermission> GetForUser(Guid userId);
        /// <summary>
        /// Returns the user's own permission tags and whether each is allowed.
        /// The default implementation performs a blocking synchronous query; implementors
        /// should override it with a genuinely asynchronous one.
        /// </summary>
        Task<List<UserMergedPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(GetForUser(userId)
                .Select(permission => new UserMergedPermission { Access = permission.IsAllowed, FeatureName = permission.Permission.Tag })
                .ToList());
        UserPermission Get(Guid userPermissionId);
        bool Any(Guid userPermissionId);
        void Add(UserPermission userPermission);
        void Remove(UserPermission userPermission);
        void Update(UserPermission userPermission);
        void SaveChanges();
        Task SaveChangesAsync();
    }
}