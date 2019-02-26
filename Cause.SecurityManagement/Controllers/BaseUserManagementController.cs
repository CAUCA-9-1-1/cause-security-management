﻿using System;
using System.Collections.Generic;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Cause.SecurityManagement.Controllers
{
	[Route("api/users")]
	public abstract class BaseUserManagementController<T> : Controller
		where T : UserManagementService
	{
		private readonly string applicationName;

		protected T UserService;

		protected BaseUserManagementController(T userService, IConfiguration configuration)
		{
			UserService = userService;
			applicationName = configuration.GetSection("APIConfig:PackageName").Value;
		}

		[HttpGet]
		public ActionResult<List<UserForEdition>> GetUsers()
		{
			return UserService.GetActiveUsers();
		}

		[HttpGet, Route("{userId:guid}")]
		public ActionResult<UserForEdition> GetUser(Guid userId)
		{
			var user = UserService.GetUser(userId);
			if (user == null)
				return NotFound();
			return user;
		}

		[HttpPost]
		public ActionResult SaveUser(UserForEdition user)
		{
			UserService.UpdateUser(user, applicationName);
			return NoContent();
		}

		[HttpDelete, Route("{userId:guid}")]
		public ActionResult DeleteUser(Guid userId)
		{
			if (UserService.DeactivateUser(userId))
				return NoContent();
			return NotFound();
		}

		[HttpGet, Route("{userId:guid}/groups")]
		public ActionResult<List<UserGroup>> GetGroups(Guid userId)
		{
			return UserService.GetGroups(userId);
		}

		[HttpPost, Route("group")]
		public ActionResult AddGroup(UserGroup userGroup)
		{
			UserService.AddGroup(userGroup);
			return NoContent();
		}

		[HttpDelete, Route("groups/{userGroupId:guid}")]
		public ActionResult RemoveGroup(Guid userGroupid)
		{
			if (UserService.RemoveGroup(userGroupid))
				return NoContent();
			return NotFound();
		}

		[HttpPost, Route("permission")]
		public ActionResult SavePermission(UserPermission permission)
		{
			UserService.UpdatePermission(permission);
			return NoContent();
		}

		[HttpDelete, Route("/permissions/{userPermissionId:guid}")]
		public ActionResult RemovePermission(Guid userPermissionId)
		{
			if (UserService.RemovePermission(userPermissionId))
				return NoContent();
			return NotFound();
		}

		[HttpPost, Route("/password")]
		public ActionResult ChangePassword([FromBody]UserPassword userPassword)
		{
			if (userPassword.PasswordConfirmation != userPassword.Password)
				return BadRequest("Password confirmation is different from password.");
			if (UserService.ChangePassword(userPassword.Id, userPassword.Password, applicationName))
				return NoContent();
			return NotFound();
		}
	}
}