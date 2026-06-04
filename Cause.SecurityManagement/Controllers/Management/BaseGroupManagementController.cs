using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers.Management;

/// <summary>
/// Abstract, fully asynchronous REST surface for the modernized group management UI: group edition
/// (upsert/delete/read), membership listing and active-user search. Subclass it in the host
/// application to activate the endpoints, e.g. <c>public class GroupManagementController(
/// IGroupManagementApiService service, IValidator&lt;GroupDto&gt; validator)
/// : BaseGroupManagementController(service, validator);</c>. Every action accepts the request's
/// <see cref="CancellationToken"/> and threads it down to the database so a cancelled request never
/// runs its query.
/// </summary>
[Route("GroupManagement")]
public abstract class BaseGroupManagementController(
    IGroupManagementApiService groupService,
    IValidator<GroupDto> groupValidator)
    : Controller
{
    protected IGroupManagementApiService GroupService = groupService;

    [HttpDelete("{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Group deleted successfully")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No group exists with the supplied identifier")]
    [SwaggerOperation(
        Summary = "Deletes a group",
        Description = "Removes the group together with its permission overrides and its membership. Requires an authenticated user.")]
    public virtual async Task<ActionResult> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await GroupService.DeleteGroupAsync(groupId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost]
    [ProducesResponseType<GroupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerResponse(StatusCodes.Status200OK, "The saved group", typeof(GroupDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The group payload is invalid", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerOperation(
        Summary = "Creates or updates a group (upsert)",
        Description = "The client generates the group and permission identifiers. The group is inserted when its identifier is unknown and updated otherwise, together with its permission overrides and membership. Requires an authenticated user.")]
    public virtual async Task<ActionResult<GroupDto>> SaveGroupAsync([FromBody] GroupDto group, CancellationToken cancellationToken)
    {
        var validation = await groupValidator.ValidateAsync(group, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        return Ok(await GroupService.SaveGroupAsync(group, cancellationToken));
    }

    [HttpGet("{groupId:guid}")]
    [ProducesResponseType<GroupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, "The full group payload", typeof(GroupDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No group exists with the supplied identifier")]
    [SwaggerOperation(
        Summary = "Retrieves a group by its identifier",
        Description = "Returns the group with its permission overrides and members. Requires an authenticated user.")]
    public virtual async Task<ActionResult<GroupDto>> GetGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GroupService.GetGroupAsync(groupId, cancellationToken);
        return group == null ? NotFound() : Ok(group);
    }
}
