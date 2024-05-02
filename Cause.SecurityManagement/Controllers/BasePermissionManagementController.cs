using System;
using System.Collections.Generic;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cause.SecurityManagement.Controllers
{
	[Route("api/permissions")]
	public abstract class BasePermissionManagementController(IPermissionManagementService permissionService)
        : Controller
    {
		protected IPermissionManagementService PermissionService = permissionService;

        [HttpGet]
		public ActionResult<List<ModulePermission>> GetPermissions()
		{
			return PermissionService.GetPermissions();
		}

        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public ActionResult Add([FromBody]ModulePermission permission)
        {
            var result = PermissionService.Add(permission);
            if (result)
                return Ok();
            return BadRequest();
        }

        [HttpPost("Update")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public ActionResult Update([FromBody]ModulePermission permission)
        {
            var result = PermissionService.Update(permission);
            if (result)
                return Ok();
            return BadRequest();
        }

        [HttpDelete("{permissionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public ActionResult Delete(Guid permissionId)
        {
            if (PermissionService.Delete(permissionId))
                return Ok();
            return BadRequest();
        }
    }
}