using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Services
{
	public class UserManagementService
	{
		protected ISecurityContext SecurityContext;

		public UserManagementService(ISecurityContext securityContext)
		{
			SecurityContext = securityContext;
		}

		public List<UserForEdition> GetActiveUsers()
		{
			return SecurityContext.Users
				.Where(user => user.IsActive)
				.Select(user => new UserForEdition
				{
					Email = user.Email,
					FirstName = user.FirstName,
					LastName = user.LastName,
					UserName = user.UserName,
					Id = user.Id
				})
				.ToList();
		}

		public UserForEdition GetUser(Guid userId)
		{
			var user = SecurityContext.Users.Find(userId);
			if (user != null)
				return new UserForEdition
				{
					Email = user.Email,
					FirstName = user.FirstName,
					LastName = user.LastName,
					UserName = user.UserName,
					Id = user.Id
				};
			return null;
		}

		public bool UpdateUser(UserForEdition user, string applicationName)
		{
			var realUser = SecurityContext.Users.Find(user.Id);
			if (realUser == null)
				realUser = GenerateNewUser(user);
			else
				PushToRealUser(user, realUser);

			if (!string.IsNullOrWhiteSpace(user.Password))
				realUser.Password = new PasswordGenerator().EncodePassword(user.Password, applicationName);

			SecurityContext.SaveChanges();
			return true;
		}

		private void PushToRealUser(UserForEdition user, User realUser)
		{
			realUser.UserName = user.UserName;
			realUser.FirstName = user.FirstName;
			realUser.LastName = user.LastName;
			realUser.Email = user.Email;
			SecurityContext.Users.Update(realUser);
		}

		private User GenerateNewUser(UserForEdition user)
		{
			var realUser = new User
			{
				Id = user.Id,
				Email = user.Email,
				IsActive = true,
				FirstName = user.FirstName,
				LastName = user.LastName,
				UserName = user.UserName
			};
			SecurityContext.Users.Add(realUser);
			return realUser;
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