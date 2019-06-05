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

		public List<ModulePermission> GetPermissions()
		{
			return SecurityContext.ModulePermissions.ToList();
		}
	}
}