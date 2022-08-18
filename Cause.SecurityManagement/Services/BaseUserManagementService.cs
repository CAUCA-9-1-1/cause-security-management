﻿using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
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
		protected readonly ISecurityContext<TUser> SecurityContext;
        private readonly IUserGroupRepository userGroupRepository;
		private readonly IUserPermissionRepository userPermissionRepository;
        private readonly IGroupPermissionRepository groupPermissionRepository;
		protected readonly SecurityConfiguration SecurityConfiguration;

		public UserManagementService(
			ISecurityContext<TUser> securityContext,
			IOptions<SecurityConfiguration> securityOptions,
            IUserGroupRepository userGroupRepository,
            IUserPermissionRepository userPermissionRepository,
            IGroupPermissionRepository groupPermissionRepository,
			IEmailForUserModificationSender emailSender = null)
		{
			SecurityContext = securityContext;
            this.emailSender = emailSender;
            this.userGroupRepository = userGroupRepository;
            this.userPermissionRepository = userPermissionRepository;
			this.groupPermissionRepository = groupPermissionRepository;
            SecurityConfiguration = securityOptions.Value;
        }

		public virtual List<TUser> GetActiveUsers()
		{
			return SecurityContext.Users
				.Where(u => u.IsActive)
                .Include(u => u.Groups)
                .Include(u => u.Permissions)
                .ToList();
		}

		public virtual TUser GetUser(Guid userId)
		{
		    return SecurityContext.Users.Find(userId);
		}

		public virtual bool UpdateUser(TUser user)
		{
			if (UserNameAlreadyUsed(user))
				return false;

            UpdateUserGroup(user);
            UpdateUserPermission(user);
            UpdatePassword(user, true);

            if (SecurityContext.Users.AsNoTracking().Any(u => u.Id == user.Id))
                SecurityContext.Users.Update(user);
            else
                SecurityContext.Users.Add(user);

            SecurityContext.SaveChanges();
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
				user.Password = new PasswordGenerator().EncodePassword(user.Password, SecurityConfiguration.PackageName);
			}
			else
				user.Password = SecurityContext.Users.AsNoTracking()
					.Where(u => u.Id == user.Id)
					.Select(u => u.Password).First();
		}

        public virtual bool UserNameAlreadyUsed(TUser user)
		{
			return SecurityContext.Users.Any(c => c.UserName == user.UserName && c.Id != user.Id && c.IsActive);
		}

		public virtual bool EmailIsAlreadyInUse(string email, Guid idUserToIgnore)
		{
			return SecurityContext.Users
				.Any(c => c.Email.ToLower() == email.ToLower() && c.Id != idUserToIgnore && c.IsActive);
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
                if (userGroups.Any(g => g.Id == userGroup.Id) == false)
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
            var user = SecurityContext.Users.Find(userId);
            if (user != null)
            {
                user.Password = new PasswordGenerator().EncodePassword(newPassword, SecurityConfiguration.PackageName);
				user.PasswordMustBeResetAfterLogin = userMustResetPasswwordAtNextLogin;
                SecurityContext.SaveChanges();
				emailSender?.SendEmailForModifiedPassword(user.Email);
				return true;
            }
            return false;
        }

        public virtual bool DeactivateUser(Guid userId)
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