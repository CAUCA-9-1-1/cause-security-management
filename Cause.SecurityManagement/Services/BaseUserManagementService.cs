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
	public class UserManagementService<TUser> : IUserManagementService<TUser> where TUser : User, new() 
	{
        private readonly IEmailForUserModificationSender emailSender;
        private readonly IUserGroupRepository userGroupRepository;
		private readonly IUserPermissionRepository userPermissionRepository;
        private readonly IGroupPermissionRepository groupPermissionRepository;
        private readonly IUserRepository<TUser> userRepository;
        private readonly IUserGroupPermissionService userGroupPermissionService;
		protected readonly SecurityConfiguration SecurityConfiguration;

        public UserManagementService(
            IOptions<SecurityConfiguration> securityOptions,
            IUserGroupRepository userGroupRepository,
            IUserPermissionRepository userPermissionRepository,
            IGroupPermissionRepository groupPermissionRepository,
			IUserRepository<TUser> userRepository,
            IUserGroupPermissionService userGroupPermissionService,
            IEmailForUserModificationSender emailSender = null)
		{
            this.emailSender = emailSender;
            this.userGroupRepository = userGroupRepository;
            this.userPermissionRepository = userPermissionRepository;
			this.groupPermissionRepository = groupPermissionRepository;
            this.userRepository = userRepository;
            this.userGroupPermissionService = userGroupPermissionService;

            SecurityConfiguration = securityOptions.Value;
        }

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

            UpdateUserGroup(user);
            UpdateUserPermission(user);
            UpdatePassword(user, true);

            if (userRepository.Any(user.Id))
                userRepository.Update(user);
            else
                userRepository.Add(user);

            userRepository.SaveChanges();
			emailSender?.SendEmailForModifiedUser(user.Email);
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
            var dbUserGroups = userGroupRepository.GetForUser(user.Id).ToList();

            dbUserGroups.ForEach(userGroup =>
            {
                if (userGroups.Any(g => g.Id == userGroup.Id) == false && userGroupPermissionService.CurrentUserHasRequiredPermissionForGroupsAccess(userGroup.IdGroup))
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
            var dbUserPermissions = userPermissionRepository.GetForUser(user.Id).ToList();

            dbUserPermissions.ForEach(userPermission =>
            {
                if (userPermissions.Any(p => p.Id == userPermission.Id) == false)
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

        public virtual bool ChangePassword(Guid userId, string newPassword, bool userMustResetPasswwordAtNextLogin)
        {
            var user = userRepository.Get(userId);
            if (user != null)
            {
                user.Password = new PasswordGenerator().EncodePassword(newPassword, SecurityConfiguration.PackageName);
				user.PasswordMustBeResetAfterLogin = userMustResetPasswwordAtNextLogin;
                userRepository.SaveChanges();
				emailSender?.SendEmailForModifiedPassword(user.Email);
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
            return userGroupRepository.GetForUser(userId).ToList();
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
			var userPermissions = GetUserPermissions(userId);
			var groupPermissions = GetUserGroupsPermission(userId);
			return new PermissionMergeTool().MergeUserAndGroupPermissions(groupPermissions, userPermissions);
		}

		private List<UserMergedPermission> GetUserGroupsPermission(Guid userId)
        {
			var userGroups = groupPermissionRepository.GetForUser(userId)
                .Select(g => new UserMergedPermission { Access = g.IsAllowed, FeatureName = g.Permission.Tag }).ToList();
			return userGroups;
		}

		private List<UserMergedPermission> GetUserPermissions(Guid userId)
		{
			var userPermissions = userPermissionRepository.GetForUser(userId)
				.Select(g => new UserMergedPermission { Access = g.IsAllowed, FeatureName = g.Permission.Tag })
				.ToList();
			return userPermissions;
		}

        public bool HasPermission(Guid userId, string permissionTag)
        {
			return GetPermissionsForUser(userId).Any(permission => permission.FeatureName == permissionTag && permission.Access);
		}
    }
}