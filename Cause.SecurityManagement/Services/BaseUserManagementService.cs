using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
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
				.Where(u => u.IsActive)
                .Include(u => u.Groups)
                .Include(u => u.Permissions)
                .ToList();
		}

		public TUser GetUser(Guid userId)
		{
		    return SecurityContext.Users.Find(userId);
		}

		public bool UpdateUser(TUser user, string applicationName)
        {
            UpdateUserGroup(user);
            UpdateUserPermission(user);

            if (!string.IsNullOrWhiteSpace(user.Password))
                user.Password = new PasswordGenerator().EncodePassword(user.Password, applicationName);

            if (SecurityContext.Users.AsNoTracking().Any(u => u.Id == user.Id))
            {
                if (string.IsNullOrWhiteSpace(user.Password))
                    user.Password = SecurityContext.Users.AsNoTracking()
                        .Where(u => u.Id == user.Id)
                        .Select(u => u.Password).First();
                SecurityContext.Users.Update(user);
            }
            else
                SecurityContext.Users.Add(user);

            SecurityContext.SaveChanges();
			return true;
		}

        private void UpdateUserGroup(User user)
        {
            if (user.Permissions is null)
            {
                return;
            }

            var userGroups = user.Groups.ToList();
            var dbUserGroups = SecurityContext.UserGroups.AsNoTracking().Where(ug => ug.IdUser == user.Id).ToList();

            dbUserGroups.ForEach(userGroup =>
            {
                if (userGroups.Any(g => g.Id == userGroup.Id) == false)
                {
                    SecurityContext.UserGroups.Remove(userGroup);
                }
            });

            userGroups.ForEach(userGroup =>
            {
                var isExistRecord = SecurityContext.UserGroups.AsNoTracking().Any(g => g.Id == userGroup.Id);

                if (!isExistRecord)
                {
                    SecurityContext.UserGroups.Add(userGroup);
                }
            });
        }

        private void UpdateUserPermission(User user)
        {
            if (user.Permissions is null)
            {
                return;
            }

            var userPermissions = user.Permissions.ToList();
            var dbUserPermissions = SecurityContext.UserPermissions.AsNoTracking().Where(up => up.IdUser == user.Id).ToList();

            dbUserPermissions.ForEach(userPermission =>
            {
                if (userPermissions.Any(p => p.Id == userPermission.Id) == false)
                {
                    SecurityContext.UserPermissions.Remove(userPermission);
                }
            });

            userPermissions.ForEach(userPermission =>
            {
                var isExistRecord = SecurityContext.UserPermissions.AsNoTracking().Any(p => p.Id == userPermission.Id);

                if (!isExistRecord)
                {
                    SecurityContext.UserPermissions.Add(userPermission);
                }
            });
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