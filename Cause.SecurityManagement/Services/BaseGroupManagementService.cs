using Cause.SecurityManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services
{
    public class BaseGroupManagementService<TUser> : IGroupManagementService
        where TUser : User, new()
    {
        private readonly ICurrentUserService currentUserService;
        private readonly IUserManagementService<TUser> userManagementService;
        private readonly SecurityConfiguration configuration;
        private readonly IGroupRepository groupRepository;
        private readonly IUserGroupRepository userGroupRepository;
        private readonly IGroupPermissionRepository groupPermissionRepository;

        public BaseGroupManagementService(
            ICurrentUserService currentUserService,
            IUserManagementService<TUser> userManagementService,
            IOptions<SecurityConfiguration> configuration,
            IGroupRepository groupRepository,
            IUserGroupRepository userGroupRepository,
            IGroupPermissionRepository groupPermissionRepository
        )
        {
            this.currentUserService = currentUserService;
            this.userManagementService = userManagementService;
            this.configuration = configuration.Value;
            this.groupRepository = groupRepository;
            this.userGroupRepository = userGroupRepository;
            this.groupPermissionRepository = groupPermissionRepository;
        }

        private bool HasRequiredPermissionForAllGroupsAccess()
        {
            return string.IsNullOrWhiteSpace(configuration.RequiredPermissionForAllGroupsAccess)
                   || userManagementService.HasPermission(currentUserService.GetUserId(), configuration.RequiredPermissionForAllGroupsAccess);
        }

        public List<Group> GetActiveGroups()
        {
            var groups = groupRepository.GetActiveGroups();

            if (!HasRequiredPermissionForAllGroupsAccess())
            {
                return groups
                    .Where(group => group.AssignableByAllUsers)
                    .ToList();
            }

            return groups;
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
            var dbGroupUsers = userGroupRepository.GetForGroup(group.Id).ToList();

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

            dbGroupPermissions.ForEach(groupPermission =>
            {
                if (groupPermissions.Any(g => g.Id == groupPermission.Id) == false)
                {
                    groupPermissionRepository.Remove(groupPermission);
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
            return userGroupRepository.GetForGroup(groupId).ToList();
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