using System.Collections.Generic;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cause.SecurityManagement.Controllers
{
	[Route("api/permissions")]
	public abstract class BasePermissionManagementController : Controller
    {
		protected IPermissionManagementService PermissionService;

		protected BasePermissionManagementController(IPermissionManagementService permissionService)
		{
            PermissionService = permissionService;
		}

		[HttpGet]
		public ActionResult<List<ModulePermission>> GetPermission()
		{
			return PermissionService.GetPermissions();
		}
	}
}