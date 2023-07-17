using Cause.SecurityManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Repositories;

namespace Cause.SecurityManagement.Services
{
    public class BaseGroupManagementService : IGroupManagementService
    {
        private readonly IGroupRepository groupRepository;
        private readonly IUserGroupRepository userGroupRepository;
        private readonly IGroupPermissionRepository groupPermissionRepository;
        private readonly IUserGroupPermissionService userGroupPermissionService;

        public BaseGroupManagementService(
            IGroupRepository groupRepository,
            IUserGroupRepository userGroupRepository,
            IGroupPermissionRepository groupPermissionRepository,
            IUserGroupPermissionService userGroupPermissionService
        )
        {
            this.groupRepository = groupRepository;
            this.userGroupRepository = userGroupRepository;
            this.groupPermissionRepository = groupPermissionRepository;
            this.userGroupPermissionService = userGroupPermissionService;
        }

        public List<Group> GetActiveGroups()
        {
            var groups = groupRepository.GetActiveGroups();

            return groups
                .Where(group => userGroupPermissionService.CurrentUserHasRequiredPermissionForGroupsAccess(group))
                .ToList();
        }

        public Group GetGroup(Guid groupId)
        {
            return groupRepository.Get(groupId);
        }

        public bool UpdateGroup(Group group)
        {
            if (GroupNameAlreadyUsed(group))
                return false;

            UpdateGroupUser(group);
            UpdateGroupPermission(group);

            AddOrUpdateGroup(group);
            return true;
        }

        private void AddOrUpdateGroup(Group group)
        {
            if (groupRepository.Any(group.Id))
                groupRepository.Update(group);
            else
                groupRepository.Add(group);

            groupRepository.SaveChanges();
        }

        public bool GroupNameAlreadyUsed(Group group)
        {
            return groupRepository.GroupNameAlreadyUsed(group);
        }

        private void UpdateGroupUser(Group group)
        {
            if (group.Users == null)
                return;

            var groupUsers = group.Users.ToList();
            var dbGroupUsers = userGroupRepository.GetForGroup(group.Id);

            dbGroupUsers.ForEach(groupUser =>
            {
                if (groupUsers.Any(g => g.Id == groupUser.Id) == false)
                {
                    userGroupRepository.Remove(groupUser);
                }
            });

            groupUsers.ForEach(groupUser =>
            {
                var isExistRecord = userGroupRepository.Any(groupUser.Id);

                if (!isExistRecord)
                {
                    userGroupRepository.Add(groupUser);
                }
            });
        }

        private void UpdateGroupPermission(Group group)
        {
            if (group.Permissions is null)
            {
                return;
            }

            var groupPermissions = group.Permissions.ToList();
            var dbGroupPermissions = groupPermissionRepository.GetForGroup(group.Id).ToList();

            dbGroupPermissions.ForEach(dbGroupPermission =>
            {
                if (!groupPermissions.Any(g => g.Id == dbGroupPermission.Id))
                {
                    groupPermissionRepository.Remove(dbGroupPermission);
                }
            });

            groupPermissions.ForEach(groupPermission =>
            {
                var isExistRecord = groupPermissionRepository.Any(groupPermission.Id);

                if (!isExistRecord)
                {
                    groupPermissionRepository.Add(groupPermission);
                }
            });
        }

        public bool DeactivateGroup(Guid groupId)
        {
            var group = groupRepository.Get(groupId);
            if (group != null)
            {
                groupRepository.Remove(group);
                groupRepository.SaveChanges();
                return true;
            }

            return false;
        }

        public List<UserGroup> GetUsers(Guid groupId)
        {
            return userGroupRepository.GetForGroup(groupId);
        }

        public bool AddUser(UserGroup userGroup)
        {
            userGroupRepository.Add(userGroup);
            userGroupRepository.SaveChanges();
            return true;
        }

        public bool RemoveUser(Guid userGroupId)
        {
            var userGroup = userGroupRepository.Get(userGroupId);
            if (userGroup != null)
            {
                userGroupRepository.Remove(userGroup);
                userGroupRepository.SaveChanges();
                return true;
            }

            return false;
        }

        public bool UpdatePermission(GroupPermission permission)
        {
            if (groupPermissionRepository.Any(permission.Id))
                groupPermissionRepository.Update(permission);
            else
                groupPermissionRepository.Add(permission);
            groupPermissionRepository.SaveChanges();
            return true;
        }

        public bool RemovePermission(Guid groupPermissionId)
        {
            var permission = groupPermissionRepository.Get(groupPermissionId);
            if (permission != null)
            {
                groupPermissionRepository.Remove(permission);
                groupPermissionRepository.SaveChanges();
                return true;
            }

            return false;
        }
    }
}