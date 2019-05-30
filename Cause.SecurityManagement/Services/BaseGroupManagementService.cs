using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

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
			return SecurityContext.Groups.ToList();
		}

		public Group GetGroup(Guid groupId)
		{
			return SecurityContext.Groups.Find(groupId);
		}

		public bool UpdateGroup(Group group)
        {
            if (SecurityContext.Groups.Any(g => g.Id == group.Id))
                SecurityContext.Groups.Update(group);
            else
                SecurityContext.Groups.Add(group);

            SecurityContext.SaveChanges();
			return true;
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