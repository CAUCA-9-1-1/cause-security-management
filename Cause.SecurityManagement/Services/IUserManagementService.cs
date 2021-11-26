using System;
using System.Collections.Generic;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Services
{
    public interface IUserManagementService<TUser>
    {
        List<TUser> GetActiveUsers();
        TUser GetUser(Guid userId);
        bool UpdateUser(TUser user);
        bool UserNameAlreadyUsed(TUser user);
        bool EmailIsAlreadyInUse(string email, Guid idUserToIgnore);
        bool ChangePassword(Guid userId, string newPassword);
        bool DeactivateUser(Guid userId);
        List<UserGroup> GetGroups(Guid userId);
        bool AddGroup(UserGroup group);
        bool RemoveGroup(Guid userGroupId);
        bool UpdatePermission(UserPermission permission);
        bool RemovePermission(Guid userPermissionId);
        List<UserMergedPermission> GetPermissionsForUser(Guid userId);
        bool HasPermission(Guid userId, string permissionTag);
    }
}
