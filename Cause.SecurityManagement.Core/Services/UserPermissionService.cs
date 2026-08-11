using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Repositories;

namespace Cause.SecurityManagement.Core.Services
{
    public class UserPermissionService(
        IGroupPermissionRepository groupPermissionRepository,
        IUserPermissionRepository userPermissionRepository)
        : IUserPermissionService
    {
        public bool HasPermission(Guid userId, string permissionTag)
        {
            return GetPermissionsForUser(userId).Exists(permission => permission.FeatureName == permissionTag && permission.Access);
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)
        {
            var permissions = await GetPermissionsForUserAsync(userId, cancellationToken);
            return permissions.Exists(permission => permission.FeatureName == permissionTag && permission.Access);
        }

        public List<UserMergedPermission> GetPermissionsForUser(Guid userId)
        {
            var userPermissions = GetUserPermissions(userId);
            var groupPermissions = GetUserGroupsPermission(userId);
            return new PermissionMergeTool().MergeUserAndGroupPermissions(groupPermissions, userPermissions);
        }

        public async Task<List<UserMergedPermission>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var userPermissions = await userPermissionRepository.GetForUserAsync(userId, cancellationToken);
            var groupPermissions = await groupPermissionRepository.GetForUserAsync(userId, cancellationToken);

            return new PermissionMergeTool().MergeUserAndGroupPermissions(groupPermissions, userPermissions);
        }

        private List<UserMergedPermission> GetUserGroupsPermission(Guid userId)
        {
            var userGroups = groupPermissionRepository.GetForUser(userId)
                .Select(g => new UserMergedPermission { Access = g.IsAllowed, FeatureName = g.Permission.Tag }).ToList();
            return userGroups;
        }

        private List<UserMergedPermission> GetUserPermissions(Guid userId)
        {
            var userPermissions = userPermissionRepository.GetForUser(userId)
                .Select(g => new UserMergedPermission { Access = g.IsAllowed, FeatureName = g.Permission.Tag })
                .ToList();
            return userPermissions;
        }
    }
}