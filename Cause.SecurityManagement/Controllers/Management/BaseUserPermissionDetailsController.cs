using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers.Management;

/// <summary>
/// Abstract endpoint returning the whole permission catalog with one user's effective state for each
/// entry, backing a management screen that answers "what can this user actually do, and why".
/// Subclass it in the host application to activate the endpoint.
///
/// Distinct from <c>GET UserManagement/{userId}/permissions</c>, which returns only the permissions
/// the user is actually assigned, without a label, without the assignments that produced them, and
/// with no way to tell "denied" from "never granted".
/// </summary>
/// <remarks>
/// Whether the caller holds the right to reach this screen at all is a tag-level question, expressed
/// declaratively by the host with <c>[UserWithPermission]</c> or
/// <c>[AdministratorOrUserWithPermission]</c> on the subclass. Whether the caller may see
/// <em>this particular user</em> is a row-level question that no attribute can express, which is what
/// <see cref="CanViewPermissionsForUserAsync"/> is for.
/// </remarks>
/// <example>
/// <code>
/// [UserWithPermission(Permissions.CanAccessUsers)]
/// public class UserPermissionDetailsController(IUserPermissionDetailReader reader, IUserVisibilityReader visibility)
///     : BaseUserPermissionDetailsController(reader)
/// {
///     protected override Task&lt;bool&gt; CanViewPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
///         => visibility.CanSeeUserAsync(userId, cancellationToken);
/// }
/// </code>
/// </example>
[Route("UserPermissionDetails")]
public abstract class BaseUserPermissionDetailsController(IUserPermissionDetailReader reader)
    : ControllerBase
{
    [HttpGet, Route("user/{userId:guid}")]
    [ProducesResponseType<List<UserPermissionDetailDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerResponse(StatusCodes.Status200OK, "Every catalog permission with the user's effective state", typeof(List<UserPermissionDetailDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "The caller may not see this user")]
    [SwaggerOperation(
        Summary = "Retrieves a user's effective permissions",
        Description = "Returns the whole permission catalog, each entry carrying the user's effective status — allowed, denied or undefined — and the group and user assignments that produced it. A single denial outranks any allowance, matching the engine that decides real access. Requires an authenticated user allowed to see the requested user.")]
    public virtual async Task<ActionResult<List<UserPermissionDetailDto>>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await CanViewPermissionsForUserAsync(userId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(await reader.GetForUserAsync(userId, cancellationToken));
    }

    /// <summary>
    /// Whether the current caller may see the permissions of <paramref name="userId"/>.
    /// </summary>
    /// <remarks>
    /// Abstract rather than defaulting to <c>true</c> on purpose. A host whose list screen already
    /// hides users outside the caller's scope would otherwise leak them here through a direct call,
    /// and a permissive default leaks silently — nothing fails, nothing is logged. Making it abstract
    /// costs a host one method and forces the decision to be made.
    /// </remarks>
    protected abstract Task<bool> CanViewPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken);
}
