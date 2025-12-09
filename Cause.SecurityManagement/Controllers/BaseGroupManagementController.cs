using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Controllers
{
	[Route("api/groups")]
	public abstract class BaseGroupManagementController(IGroupManagementService groupService) : Controller
    {
		protected IGroupManagementService GroupService = groupService;

        [HttpGet]
        [ProducesResponseType<List<Group>>(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "The list of active groups", typeof(List<Group>))]
        [SwaggerOperation(Summary = "Retrieves a list of all active groups")]
		public virtual ActionResult<List<Group>> GetGroups()
		{
			return GroupService.GetActiveGroups();
		}

		[HttpGet, Route("{groupId:guid}")]
        [ProducesResponseType<Group>(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "The group details", typeof(Group))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Group not found")]
        [SwaggerOperation(Summary = "Retrieves a specific group by its ID")]
		public virtual ActionResult<Group> GetUser(Guid groupId)
		{
			var group = GroupService.GetGroup(groupId);
			if (group == null)
				return NotFound();
			return group;
		}

		[HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Group saved successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid group data")]
        [SwaggerOperation(Summary = "Creates a new group or updates an existing one")]
		public virtual ActionResult SaveGroup(Group group)
		{
			var groupUpdated = GroupService.UpdateGroup(group);
			if (groupUpdated)
				return NoContent();
			return BadRequest();
		}

		[HttpDelete, Route("{groupId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Group deleted successfully")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Group not found")]
        [SwaggerOperation(Summary = "Deactivates (deletes) a group by its ID")]
		public virtual ActionResult DeleteGroup(Guid groupId)
		{
			if (GroupService.DeactivateGroup(groupId))
				return NoContent();
			return NotFound();
		}

		[HttpGet, Route("{groupId:guid}/users")]
        [ProducesResponseType<List<UserGroup>>(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "The list of users in the group", typeof(List<UserGroup>))]
        [SwaggerOperation(Summary = "Retrieves the list of users belonging to a specific group")]
		public virtual ActionResult<List<UserGroup>> GetUsers(Guid groupId)
		{
			return GroupService.GetUsers(groupId);
		}

		[HttpPost, Route("permission")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Permission saved successfully")]
        [SwaggerOperation(Summary = "Adds or updates a permission for a specific group")]
		public virtual ActionResult SavePermission(GroupPermission permission)
		{
            GroupService.UpdatePermission(permission);
			return NoContent();
		}

		[HttpDelete, Route("permissions/{groupPermissionId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Permission removed successfully")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Permission not found")]
        [SwaggerOperation(Summary = "Removes a permission from a group")]
		public virtual ActionResult RemovePermission(Guid groupPermissionId)
		{
			if (GroupService.RemovePermission(groupPermissionId))
				return NoContent();
			return NotFound();
		}

		[HttpPost, Route("GroupNameAlreadyExist")]
        [ProducesResponseType<bool>(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status200OK, "True if the group name exists, false otherwise", typeof(bool))]
        [SwaggerOperation(Summary = "Checks if a group name is already in use")]
		public virtual ActionResult GroupNameAlreadyExist([FromBody]Group group)
		{
			return Ok(GroupService.GroupNameAlreadyUsed(group));
		}
	}
}