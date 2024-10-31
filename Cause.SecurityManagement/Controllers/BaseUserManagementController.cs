using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Controllers;

[Route("api/users")]
public abstract class BaseUserManagementController<TService, TUser>(TService userService) : Controller
    where TService : IUserManagementService<TUser>
    where TUser : User, new()
{
    protected TService UserService = userService;

    [HttpGet]
    public ActionResult<List<TUser>> GetUsers()
    {
        return UserService.GetActiveUsers();
    }

    [HttpGet, Route("{userId:guid}")]
    public virtual ActionResult<TUser> GetUser(Guid userId)
    {
        var user = UserService.GetUser(userId);
        if (user == null)
            return NotFound();
        return user;
    }

    [HttpPost, Authorize(Roles = SecurityRoles.UserAndUserCreation)]
    public virtual ActionResult SaveUser(TUser user)
    {
        var userSaved = UserService.UpdateUser(user);
        if (userSaved)
            return NoContent();
        return BadRequest();
    }

    [HttpDelete, Route("{userId:guid}")]
    public virtual ActionResult DeleteUser(Guid userId)
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

    [HttpDelete, Route("permissions/{userPermissionId:guid}")]
    public ActionResult RemovePermission(Guid userPermissionId)
    {
        if (UserService.RemovePermission(userPermissionId))
            return NoContent();
        return NotFound();
    }

    [HttpPost, Route("password"), Authorize(Roles = SecurityRoles.UserAndUserRecovery)]
    public virtual ActionResult ChangePassword([FromBody]UserPassword userPassword)
    {
        if (userPassword.PasswordConfirmation != userPassword.Password)
            return BadRequest("Password confirmation is different from password.");
        if (UserService.ChangePassword(userPassword.Id, userPassword.Password, false))
            return NoContent();
        return NotFound();
    }

    [HttpGet, Route("{userId:guid}/permissions")]
    public ActionResult<List<UserMergedPermission>> GetPermissionsForUser(Guid userId)
    {
        return Ok(UserService.GetPermissionsForUser(userId));
    }

    [HttpPost, Route("UserNameAlreadyExist")]
    public virtual ActionResult UserNameAlreadyExist([FromBody]TUser user)
    {
        return Ok(UserService.UserNameAlreadyUsed(user));
    }

    [HttpPost, Route("EmailIsAlreadyInUse")]
    public virtual ActionResult EmailIsAlreadyInUse([FromBody] TUser user)
    {
        return Ok(UserService.EmailIsAlreadyInUse(user.Email, user.Id));
    }
}