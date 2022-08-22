using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Repositories;

namespace Cause.SecurityManagement.Services
{
    public class UserPermissionService : IUserPermissionService
    {
        private readonly IGroupPermissionRepository groupPermissionRepository;
        private readonly IUserPermissionRepository userPermissionRepository;

        public UserPermissionService(
            IGroupPermissionRepository groupPermissionRepository,
            IUserPermissionRepository userPermissionRepository)
        {
            this.groupPermissionRepository = groupPermissionRepository;
            this.userPermissionRepository = userPermissionRepository;
        }

        public bool HasPermission(Guid userId, string permissionTag)
        {
            return GetPermissionsForUser(userId).Any(permission => permission.FeatureName == permissionTag && permission.Access);
        }

        public List<UserMergedPermission> GetPermissionsForUser(Guid userId)
        {
            var userPermissions = GetUserPermissions(userId);
            var groupPermissions = GetUserGroupsPermission(userId);
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
