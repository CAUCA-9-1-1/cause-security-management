using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Services
{
	public class BasePermissionManagementService<TUser> : IPermissionManagementService
        where TUser : User, new()
    {
		protected ISecurityContext<TUser> SecurityContext;

		public BasePermissionManagementService(ISecurityContext<TUser> securityContext)
		{
			SecurityContext = securityContext;
		}

        public bool Add(ModulePermission permission)
        {
            if (string.IsNullOrEmpty(permission.Name) || string.IsNullOrEmpty(permission.Tag))
                return false;
            var modulePermission = new ModulePermission()
            {
                Id = Guid.NewGuid(),
                IdModule = Guid.Parse("1c365d12-a809-11e9-9916-525400cc3eb9"),
                Tag = permission.Tag,
                Name = permission.Name
            };
            SecurityContext.ModulePermissions.Add(modulePermission);
            SecurityContext.SaveChanges();
            return true;
        }

        public bool Delete(Guid permissionId)
        {
            var permissionToDelete = SecurityContext.ModulePermissions.FirstOrDefault(c => c.Id == permissionId);
            if (permissionToDelete != null)
            {
                SecurityContext.ModulePermissions.Remove(permissionToDelete);
                SecurityContext.SaveChanges();
                return true;
            }
            return false;
        }

        public List<ModulePermission> GetPermissions()
		{
			return SecurityContext.ModulePermissions.ToList();
		}

        public bool Update(ModulePermission permission)
        {
            var permissionToChange = SecurityContext.ModulePermissions.FirstOrDefault(c => c.Id == permission.Id);
            if (permissionToChange != null)
            {
                permissionToChange.Name = permission.Name;
                permissionToChange.Tag = permission.Tag;
                SecurityContext.ModulePermissions.Update(permissionToChange);
                SecurityContext.SaveChanges();
                return true;
            }
            return false;
        }
    }
}