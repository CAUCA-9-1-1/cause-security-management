using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
    [ProducesResponseType<List<User>>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The list of active users", typeof(List<User>))]
    [SwaggerOperation(Summary = "Retrieves a list of all active users")]
    public ActionResult<List<TUser>> GetUsers()
    {
        return UserService.GetActiveUsers();
    }

    [HttpGet, Route("{userId:guid}")]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The user details", typeof(User))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found")]
    [SwaggerOperation(Summary = "Retrieves a specific user by their ID")]
    public virtual ActionResult<TUser> GetUser(Guid userId)
    {
        var user = UserService.GetUser(userId);
        if (user == null)
            return NotFound();
        return user;
    }

    [HttpPost, Authorize(Roles = SecurityRoles.UserAndUserCreation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "User saved successfully")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid user data")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
    [SwaggerOperation(
        Summary = "Creates a new user or updates an existing one",
        Description = "Requires one of the following roles: RegularUser, UserCreation")]
    public virtual ActionResult SaveUser(TUser user)
    {
        var userSaved = UserService.UpdateUser(user);
        if (userSaved)
            return NoContent();
        return BadRequest();
    }

    [HttpDelete, Route("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "User deleted successfully")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found")]
    [SwaggerOperation(Summary = "Deactivates (deletes) a user by their ID")]
    public virtual ActionResult DeleteUser(Guid userId)
    {
        if (UserService.DeactivateUser(userId))
            return NoContent();
        return NotFound();
    }

    [HttpGet, Route("{userId:guid}/groups")]
    [ProducesResponseType<List<UserGroup>>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The list of groups for the user", typeof(List<UserGroup>))]
    [SwaggerOperation(Summary = "Retrieves the list of groups a user belongs to")]
    public ActionResult<List<UserGroup>> GetGroups(Guid userId)
    {
        return UserService.GetGroups(userId);
    }

    [HttpPost, Route("group")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "User added to group successfully")]
    [SwaggerOperation(Summary = "Adds a user to a group")]
    public ActionResult AddGroup(UserGroup userGroup)
    {
        UserService.AddGroup(userGroup);
        return NoContent();
    }

    [HttpDelete, Route("groups/{userGroupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "User removed from group successfully")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User group association not found")]
    [SwaggerOperation(Summary = "Removes a user from a group")]
    public ActionResult RemoveGroup(Guid userGroupid)
    {
        if (UserService.RemoveGroup(userGroupid))
            return NoContent();
        return NotFound();
    }

    [HttpPost, Route("permission")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Permission saved successfully")]
    [SwaggerOperation(Summary = "Adds or updates a specific permission for a user")]
    public ActionResult SavePermission(UserPermission permission)
    {
        UserService.UpdatePermission(permission);
        return NoContent();
    }

    [HttpDelete, Route("permissions/{userPermissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Permission removed successfully")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Permission not found")]
    [SwaggerOperation(Summary = "Removes a specific permission from a user")]
    public ActionResult RemovePermission(Guid userPermissionId)
    {
        if (UserService.RemovePermission(userPermissionId))
            return NoContent();
        return NotFound();
    }

    [HttpPost, Route("password"), Authorize(Roles = SecurityRoles.UserAndUserRecovery)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Password changed successfully")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Password confirmation mismatch")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
    [SwaggerOperation(
        Summary = "Changes the password for a user",
        Description = "Requires one of the following roles: RegularUser, UserRecovery")]
    public virtual ActionResult ChangePassword([FromBody]UserPassword userPassword)
    {
        if (userPassword.PasswordConfirmation != userPassword.Password)
            return BadRequest("Password confirmation is different from password.");
        if (UserService.ChangePassword(userPassword.Id, userPassword.Password, false, userPassword.CurrentPassword))
            return NoContent();
        return NotFound();
    }

    [HttpGet, Route("{userId:guid}/permissions")]
    [ProducesResponseType<List<UserMergedPermission>>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The list of merged permissions for the user", typeof(List<UserMergedPermission>))]
    [SwaggerOperation(Summary = "Retrieves the effective permissions for a user (merged from user and group permissions)")]
    public ActionResult<List<UserMergedPermission>> GetPermissionsForUser(Guid userId)
    {
        return Ok(UserService.GetPermissionsForUser(userId));
    }

    [HttpPost, Route("UserNameAlreadyExist")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "True if the username exists, false otherwise", typeof(bool))]
    [SwaggerOperation(Summary = "Checks if a username is already taken")]
    public virtual ActionResult UserNameAlreadyExist([FromBody]TUser user)
    {
        return Ok(UserService.UserNameAlreadyUsed(user));
    }

    [HttpPost, Route("EmailIsAlreadyInUse")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "True if the email is in use, false otherwise", typeof(bool))]
    [SwaggerOperation(Summary = "Checks if an email address is already associated with another user")]
    public virtual ActionResult EmailIsAlreadyInUse([FromBody] TUser user)
    {
        return Ok(UserService.EmailIsAlreadyInUse(user.Email, user.Id));
    }
}