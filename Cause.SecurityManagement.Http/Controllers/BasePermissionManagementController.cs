using System;
using System.Collections.Generic;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers
{
	[Route("api/permissions")]
	public abstract class BasePermissionManagementController(IPermissionManagementService permissionService)
        : Controller
    {
		protected IPermissionManagementService PermissionService = permissionService;

        [HttpGet]
        [ProducesResponseType<List<ModulePermission>>(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "The list of module permissions", typeof(List<ModulePermission>))]
        [SwaggerOperation(Summary = "Retrieves a list of all available module permissions")]
		public ActionResult<List<ModulePermission>> GetPermissions()
		{
			return PermissionService.GetPermissions();
		}

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "Permission added successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid permission data")]
        [SwaggerOperation(Summary = "Adds a new module permission")]
        public ActionResult Add([FromBody]ModulePermission permission)
        {
            var result = PermissionService.Add(permission);
            if (result)
                return Ok();
            return BadRequest();
        }

        [HttpPost("Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "Permission updated successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid permission data")]
        [SwaggerOperation(Summary = "Updates an existing module permission")]
        public ActionResult Update([FromBody]ModulePermission permission)
        {
            var result = PermissionService.Update(permission);
            if (result)
                return Ok();
            return BadRequest();
        }

        [HttpDelete("{permissionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "Permission deleted successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid permission ID")]
        [SwaggerOperation(Summary = "Deletes a module permission by its ID")]
        public ActionResult Delete(Guid permissionId)
        {
            if (PermissionService.Delete(permissionId))
                return Ok();
            return BadRequest();
        }
    }
}