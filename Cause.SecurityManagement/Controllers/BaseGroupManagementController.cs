using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Controllers
{
	[Route("api/groups")]
	public abstract class BaseGroupManagementController(IGroupManagementService groupService) : Controller
    {
		protected IGroupManagementService GroupService = groupService;

        [HttpGet]
		public virtual ActionResult<List<Group>> GetGroups()
		{
			return GroupService.GetActiveGroups();
		}

		[HttpGet, Route("{groupId:guid}")]
		public virtual ActionResult<Group> GetUser(Guid groupId)
		{
			var group = GroupService.GetGroup(groupId);
			if (group == null)
				return NotFound();
			return group;
		}

		[HttpPost]
		public virtual ActionResult SaveGroup(Group group)
		{
			var groupUpdated = GroupService.UpdateGroup(group);
			if (groupUpdated)
				return NoContent();
			return BadRequest();
		}

		[HttpDelete, Route("{groupId:guid}")]
		public virtual ActionResult DeleteGroup(Guid groupId)
		{
			if (GroupService.DeactivateGroup(groupId))
				return NoContent();
			return NotFound();
		}

		[HttpGet, Route("{groupId:guid}/users")]
		public virtual ActionResult<List<UserGroup>> GetUsers(Guid groupId)
		{
			return GroupService.GetUsers(groupId);
		}

		[HttpPost, Route("permission")]
		public virtual ActionResult SavePermission(GroupPermission permission)
		{
            GroupService.UpdatePermission(permission);
			return NoContent();
		}

		[HttpDelete, Route("permissions/{groupPermissionId:guid}")]
		public virtual ActionResult RemovePermission(Guid groupPermissionId)
		{
			if (GroupService.RemovePermission(groupPermissionId))
				return NoContent();
			return NotFound();
		}

		[HttpPost, Route("GroupNameAlreadyExist")]
		public virtual ActionResult GroupNameAlreadyExist([FromBody]Group group)
		{
			return Ok(GroupService.GroupNameAlreadyUsed(group));
		}
	}
}