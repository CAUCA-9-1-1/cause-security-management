using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Services
{
	public class UserManagementService<TUser> where TUser : User, new()
	{
		protected ISecurityContext<TUser> SecurityContext;

		public UserManagementService(ISecurityContext<TUser> securityContext)
		{
			SecurityContext = securityContext;
		}

		public List<TUser> GetActiveUsers()
		{
			return SecurityContext.Users
				.Where(user => user.IsActive)
                .ToList();
		}

		public TUser GetUser(Guid userId)
		{
		    return SecurityContext.Users.Find(userId);
		}

		public bool UpdateUser(TUser user, string applicationName)
		{
            if (!string.IsNullOrWhiteSpace(user.Password))
                user.Password = new PasswordGenerator().EncodePassword(user.Password, applicationName);

            if (SecurityContext.Users.Any(u => u.Id == user.Id))
                SecurityContext.Users.Update(user);
            else
                SecurityContext.Users.Add(user);

            SecurityContext.SaveChanges();
			return true;
		}

        public bool ChangePassword(Guid userId, string newPassword, string applicationName)
        {
            var user = SecurityContext.Users.Find(userId);
            if (user != null)
            {
                user.Password = new PasswordGenerator().EncodePassword(newPassword, applicationName);
                SecurityContext.SaveChanges();
                return true;
            }
            return false;
        }

        public bool DeactivateUser(Guid userId)
		{
			var user = SecurityContext.Users.Find(userId);
			if (user != null)
			{
				user.IsActive = false;
				SecurityContext.SaveChanges();
				return true;
			}

			return false;
		}

		public List<UserGroup> GetGroups(Guid userId)
		{
			return SecurityContext.UserGroups
				.Where(group => group.IdUser == userId)
				.ToList();
		}

		public bool AddGroup(UserGroup group)
		{
			SecurityContext.Add(group);
			SecurityContext.SaveChanges();
			return true;
		}

		public bool RemoveGroup(Guid userGroupId)
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

		public bool UpdatePermission(UserPermission permission)
		{
			if (SecurityContext.UserPermissions.Any(u => u.Id == permission.Id))
				SecurityContext.UserPermissions.Update(permission);
			else
				SecurityContext.UserPermissions.Add(permission);
			SecurityContext.SaveChanges();
			return true;
		}

		public bool RemovePermission(Guid userPermissionId)
		{
			var permission = SecurityContext.UserPermissions.Find(userPermissionId);
			if (permission != null)
			{
				SecurityContext.UserPermissions.Remove(permission);
				SecurityContext.SaveChanges();
				return true;
			}

			return false;
		}
	}
}