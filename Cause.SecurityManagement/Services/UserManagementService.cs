using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services
{
	public class UserManagementService<TUser>(
        IOptions<SecurityConfiguration> securityOptions,
        IUserGroupRepository userGroupRepository,
        IUserPermissionRepository userPermissionRepository,
        IUserRepository<TUser> userRepository,
        IUserGroupPermissionService userGroupPermissionService,
        IUserPermissionService userPermissionService,
        IEmailForUserModificationSender emailSender = null)
        : IUserManagementService<TUser>
        where TUser : User, new()
    {
        protected readonly SecurityConfiguration SecurityConfiguration = securityOptions.Value;

        public virtual List<TUser> GetActiveUsers()
        {
            return userRepository.GetActiveUsers().ToList();
		}

		public virtual TUser GetUser(Guid userId)
		{
		    return userRepository.Get(userId);
		}

		public virtual bool UpdateUser(TUser user)
		{
            
			if (UserNameAlreadyUsed(user))
				return false;

            if (userRepository.Any(user.Id))
                userRepository.Update(user);
            else
                userRepository.Add(user);

            UpdateUserGroup(user);
            UpdateUserPermission(user);
            UpdatePassword(user, true);

            userRepository.SaveChanges();
            if (user.IsActive)
            {
                emailSender?.SendEmailForModifiedUser(user.Email);
            }
            return true;
		}

        public virtual void UpdatePassword(TUser user, bool userMustResetPasswordWhenPasswordIsChanged)
        {
            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                if (userMustResetPasswordWhenPasswordIsChanged)
                {
                    user.PasswordMustBeResetAfterLogin = true;
                }

                user.Password =
                    new PasswordGenerator().EncodePassword(user.Password, SecurityConfiguration.PackageName);
            }
            else
                user.Password = userRepository.GetPassword(user.Id);
		}

        public virtual bool UserNameAlreadyUsed(TUser user)
		{
			return userRepository.UserNameAlreadyUsed(user);
		}

		public virtual bool EmailIsAlreadyInUse(string email, Guid idUserToIgnore)
        {
            return userRepository.EmailIsAlreadyInUse(email, idUserToIgnore);
        }

        public virtual void UpdateUserGroup(User user)
        {
            if (user.Groups is null)
            {
                return;
            }

            var userGroups = user.Groups.ToList();
            var dbUserGroups = userGroupRepository.GetForUser(user.Id);

            dbUserGroups.ForEach(userGroup =>
            {
                if (!userGroups.Exists(g => g.Id == userGroup.Id) && userGroupPermissionService.CurrentUserHasRequiredPermissionForGroupsAccess(userGroup.IdGroup))
                {
                    userGroupRepository.Remove(userGroup);
                }
            });

            userGroups.ForEach(userGroup =>
            {
                var isExistRecord = userGroupRepository.Any(userGroup.Id);

                if (!isExistRecord)
                {
                    userGroupRepository.Add(userGroup);
                }
            });
        }

        public virtual void UpdateUserPermission(User user)
        {
            if (user.Permissions is null)
            {
                return;
            }

            var userPermissions = user.Permissions.ToList();
            var dbUserPermissions = userPermissionRepository.GetForUser(user.Id);

            dbUserPermissions.ForEach(userPermission =>
            {
                if (!userPermissions.Exists(p => p.Id == userPermission.Id))
                {
                    userPermissionRepository.Remove(userPermission);
                }
            });

            userPermissions.ForEach(userPermission =>
            {
                var isExistRecord = userPermissionRepository.Any(userPermission.Id);

                if (!isExistRecord)
                {
                    userPermissionRepository.Add(userPermission);
                }
            });
        }

        public virtual bool ChangePassword(Guid userId, string newPassword, bool userMustResetPasswordAtNextLogin)
        {
            var user = userRepository.Get(userId);
            if (user != null)
            {
                user.Password = new PasswordGenerator().EncodePassword(newPassword, SecurityConfiguration.PackageName);
				user.PasswordMustBeResetAfterLogin = userMustResetPasswordAtNextLogin;
                userRepository.SaveChanges();
                if (user.IsActive)
                {
                    emailSender?.SendEmailForModifiedPassword(user.Email);
                }
                return true;
            }
            return false;
        }

        public virtual bool DeactivateUser(Guid userId)
		{
			var user = userRepository.Get(userId);
			if (user != null)
			{
				user.IsActive = false;
                userRepository.SaveChanges();
				return true;
			}

			return false;
		}

		public virtual List<UserGroup> GetGroups(Guid userId)
        {
            return userGroupRepository.GetForUser(userId);
		}

		public virtual bool AddGroup(UserGroup userGroup)
		{
			userGroupRepository.Add(userGroup);
            userGroupRepository.SaveChanges();
			return true;
		}

		public virtual bool RemoveGroup(Guid userGroupId)
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

		public virtual bool UpdatePermission(UserPermission permission)
		{
			if (userPermissionRepository.Any(permission.Id))
                userPermissionRepository.Update(permission);
			else
                userPermissionRepository.Add(permission);
            userPermissionRepository.SaveChanges();
			return true;
		}

		public virtual bool RemovePermission(Guid userPermissionId)
		{
			var permission = userPermissionRepository.Get(userPermissionId);
			if (permission != null)
			{
                userPermissionRepository.Remove(permission);
                userPermissionRepository.SaveChanges();
				return true;
			}

			return false;
		}

		public virtual List<UserMergedPermission> GetPermissionsForUser(Guid userId)
        {
            return userPermissionService.GetPermissionsForUser(userId);
		}

        public bool HasPermission(Guid userId, string permissionTag)
        {
            return userPermissionService.HasPermission(userId, permissionTag);
		}
    }
}