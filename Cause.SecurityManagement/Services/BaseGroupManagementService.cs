using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cause.SecurityManagement.Services
{
	public class BaseGroupManagementService<TUser> : IGroupManagementService
        where TUser : User, new()
    {
		protected ISecurityContext<TUser> SecurityContext;

		public BaseGroupManagementService(ISecurityContext<TUser> securityContext)
		{
			SecurityContext = securityContext;
		}

		public List<Group> GetActiveGroups()
		{
			return SecurityContext.Groups
                .Include(g => g.Users)
                .Include(g => g.Permissions)
                .ToList();
		}

		public Group GetGroup(Guid groupId)
		{
			return SecurityContext.Groups.Find(groupId);
		}

		public bool UpdateGroup(Group group)
		{
			if (GroupNameAlreadyUsed(group))
				return false;

            UpdateGroupUser(group);
            UpdateGroupPermission(group);

            if (SecurityContext.Groups.AsNoTracking().Any(g => g.Id == group.Id))
                SecurityContext.Groups.Update(group);
            else
                SecurityContext.Groups.Add(group);

            SecurityContext.SaveChanges();
			return true;
		}

		public bool GroupNameAlreadyUsed(Group group)
		{
			return SecurityContext.Groups.Any(c => c.Name == group.Name && c.Id != group.Id);
		}

		private void UpdateGroupUser(Group group)
        {
            if (group.Users == null)
	            return;

            var groupUsers = group.Users.ToList();
            var dbGroupUsers = SecurityContext.UserGroups.AsNoTracking().Where(uc => uc.IdGroup == group.Id).ToList();

            dbGroupUsers.ForEach(groupUser =>
            {
                if (groupUsers.Any(g => g.Id == groupUser.Id) == false)
                {
                    SecurityContext.UserGroups.Remove(groupUser);
                }
            });

            groupUsers.ForEach(groupUser =>
            {
                var isExistRecord = SecurityContext.UserGroups.AsNoTracking().Any(g => g.Id == groupUser.Id);

                if (!isExistRecord)
                {
                    SecurityContext.UserGroups.Add(groupUser);
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
            var dbGroupPermissions = SecurityContext.GroupPermissions.AsNoTracking().Where(uc => uc.IdGroup == group.Id).ToList();

            dbGroupPermissions.ForEach(groupPermission =>
            {
                if (groupPermissions.Any(g => g.Id == groupPermission.Id) == false)
                {
                    SecurityContext.GroupPermissions.Remove(groupPermission);
                }
            });

            groupPermissions.ForEach(groupPermission =>
            {
                var isExistRecord = SecurityContext.GroupPermissions.AsNoTracking().Any(g => g.Id == groupPermission.Id);

                if (!isExistRecord)
                {
                    SecurityContext.GroupPermissions.Add(groupPermission);
                }
            });
        }

        public bool DeactivateGroup(Guid groupId)
		{
			var group = SecurityContext.Groups.Find(groupId);
			if (group != null)
			{
                SecurityContext.Groups.Remove(group);
                SecurityContext.SaveChanges();
				return true;
			}

			return false;
		}

		public List<UserGroup> GetUsers(Guid groupId)
		{
			return SecurityContext.UserGroups
				.Where(group => group.IdGroup == groupId)
				.ToList();
		}

		public bool AddUser(UserGroup user)
		{
			SecurityContext.Add(user);
			SecurityContext.SaveChanges();
			return true;
		}

		public bool RemoveUser(Guid userGroupId)
		{
			var group = SecurityContext.UserGroups.Find(userGroupId);
			if (group != null)
			{
				SecurityContext.UserGroups.Remove(group);
				SecurityContext.SaveChanges();
				return true;
			}

			return false;
		}

		public bool UpdatePermission(GroupPermission permission)
		{
			if (SecurityContext.GroupPermissions.Any(u => u.Id == permission.Id))
				SecurityContext.GroupPermissions.Update(permission);
			else
				SecurityContext.GroupPermissions.Add(permission);
			SecurityContext.SaveChanges();
			return true;
		}

		public bool RemovePermission(Guid groupPermissionId)
		{
			var permission = SecurityContext.GroupPermissions.Find(groupPermissionId);
			if (permission != null)
			{
				SecurityContext.GroupPermissions.Remove(permission);
				SecurityContext.SaveChanges();
				return true;
			}

			return false;
		}
	}
}