using Cause.SecurityManagement.Models;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Services
{
	public interface IGroupManagementService
    {
        List<Group> GetActiveGroups();
        Group GetGroup(Guid groupId);
        bool UpdateGroup(Group group);
        bool DeactivateGroup(Guid groupId);
        List<UserGroup> GetUsers(Guid groupId);
        bool AddUser(UserGroup userGroup);
        bool RemoveUser(Guid userGroupId);
        bool UpdatePermission(GroupPermission permission);
        bool RemovePermission(Guid groupPermissionId);
        bool GroupNameAlreadyUsed(Group group);
    }
}