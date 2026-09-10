using System.Collections.Generic;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers.Management;

/// <summary>
/// Abstract create/read/update endpoint for one user's edition form. Subclass it in the host
/// application to activate it; the host owns its own <c>ForEdition</c> shape, because the fields a
/// user carries beyond the shared ones differ per site.
/// </summary>
/// <remarks>
/// The route declared here is unprefixed. A host reaching its management endpoints under a prefix
/// re-declares <c>[Route("api/[Controller]")]</c> on the subclass, which overrides this one.
/// Omitting it there serves the endpoint at the root and the SPA gets a 404.
///
/// <para>
/// Subclassing this controller with no permission attribute exposes it to every authenticated
/// caller: the project's default authorization convention only requires an authenticated principal,
/// it does not gate individual endpoints. Whether the caller may reach the screen at all is a
/// tag-level question the host answers declaratively with <c>[UserWithPermission]</c> or
/// <c>[AdministratorOrUserWithPermission]</c> on the subclass; whether the caller may act on
/// <em>this particular user</em>, or grant <em>these particular groups</em>, is a row-level question
/// that no attribute can express, which is what <see cref="CanEditUserAsync"/> and
/// <see cref="CanGrantGroupsAsync"/> are for. Both gates are expected together on this controller.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [UserWithPermission(Permissions.CanAccessUsers)]
/// public class UserEditionController(
///     IUserNameAvailabilityReader userNameAvailability,
///     IValidator&lt;UserForEdition&gt; userValidator,
///     IUserVisibilityReader visibility,
///     IUserManagementApiService service)
///     : BaseUserEditionController&lt;User, UserForEdition&gt;(userNameAvailability, userValidator)
/// {
///     protected override Task&lt;bool&gt; CanEditUserAsync(Guid userId, CancellationToken cancellationToken)
///         => visibility.CanSeeUserAsync(userId, cancellationToken);
///
///     protected override Task&lt;bool&gt; CanGrantGroupsAsync(IReadOnlyCollection&lt;Guid&gt; groupIds, CancellationToken cancellationToken)
///         => visibility.CanGrantGroupsAsync(groupIds, cancellationToken);
///
///     protected override Task&lt;UserForEdition&gt; ReadForEditionAsync(Guid userId, CancellationToken cancellationToken)
///         => service.ReadForEditionAsync(userId, cancellationToken);
///
///     protected override Task&lt;UserSaveResult&gt; WriteForEditionAsync(UserForEdition user, CancellationToken cancellationToken)
///         => service.WriteForEditionAsync(user, cancellationToken);
/// }
/// </code>
/// </example>
[Route("UserEdition")]
public abstract class BaseUserEditionController<TUser, TUserForEdition>(
    IUserNameAvailabilityReader userNameAvailability,
    IValidator<TUserForEdition> userValidator)
    : ControllerBase
    where TUser : User, new()
    where TUserForEdition : UserForEdition
{
    private const string SaveFailedKey = "users.edition.saveFailed";

    [HttpGet, Route("{userId:guid}")]
    [ProducesResponseType<UserForEdition>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerResponse(StatusCodes.Status200OK, "The user in its edition shape", typeof(UserForEdition))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "The caller may not see this user")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No active user carries this identifier")]
    [SwaggerOperation(
        Summary = "Retrieves a user in its edition shape",
        Description = "Requires an authenticated caller allowed to see the requested user. Deactivated users are not returned. A caller outside the user's scope receives 403 rather than 404, so the status cannot be used to discover which identifiers exist.")]
    public virtual async Task<ActionResult<TUserForEdition>> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await CanEditUserAsync(userId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var user = await ReadForEditionAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK, "The user was created; the body is the new identifier", typeof(Guid))]
    [SwaggerResponse(StatusCodes.Status204NoContent, "The user was updated")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The body is missing or failed validation; the errors name the offending property paths", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "The caller may not edit this user, or may not grant one of the requested groups")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The identifier names a user that does not exist")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "The save failed; detail carries a translation key")]
    [SwaggerOperation(
        Summary = "Creates or updates a user",
        Description = "An empty identifier creates, a populated one updates; a create returns 200 with the new identifier, an update returns 204. The body is validated by the injected IValidator<TUserForEdition> before anything else runs; failures are RFC 7807 with the validator's own failure messages, which are plain FluentValidation text unless the host configures translation-key messages on its rules. Requires an authenticated caller allowed to edit the requested user and allowed to grant every requested group, checked on both creation and update.")]
    public virtual async Task<ActionResult> SaveAsync([FromBody] TUserForEdition user, CancellationToken cancellationToken)
    {
        if (user is null)
        {
            ModelState.AddModelError(nameof(user), "The request body must not be empty.");
            return ValidationProblem(ModelState);
        }

        var validation = await userValidator.ValidateAsync(user, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        if (!await CanEditUserAsync(user.Id, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!await CanGrantGroupsAsync(user.GroupIds, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var isCreating = user.Id == Guid.Empty;
        var result = await WriteForEditionAsync(user, cancellationToken);

        return result.Outcome switch
        {
            UserSaveOutcome.Done when isCreating => Ok(result.Id),
            UserSaveOutcome.Done => NoContent(),
            UserSaveOutcome.NotFound => NotFound(),
            _ => Problem(detail: SaveFailedKey, statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    [HttpGet, Route("username-available")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerResponse(StatusCodes.Status200OK, "True when the name may be claimed", typeof(bool))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerOperation(
        Summary = "Checks whether a user name is free",
        Description = "Deactivated users do not hold their name. Pass the identifier of the user being edited so an unchanged name is not reported as taken; omit it when creating. Advisory only: this base does not call IUserNameAvailabilityReader from the save path, and the answer is racy against a concurrent write. The host's WriteForEditionAsync must re-check uniqueness itself before writing.")]
    public virtual async Task<ActionResult<bool>> IsUserNameAvailableAsync(
        [FromQuery] string userName,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken)
    {
        return Ok(await userNameAvailability.IsAvailableAsync(userName, userId ?? Guid.Empty, cancellationToken));
    }

    /// <summary>Whether the current caller may edit <paramref name="userId"/>, including creating a new user.</summary>
    /// <remarks>
    /// Called unconditionally by <see cref="SaveAsync"/>, for both creation and update. <paramref name="userId"/>
    /// is <see cref="Guid.Empty"/> on creation, so implementing this method answers, in that case,
    /// "may this caller create a user" rather than "may this caller edit an existing one". A host that
    /// wants every authenticated caller to create users as before — the behavior this method replaced —
    /// writes that decision explicitly in its own override, e.g. <c>userId == Guid.Empty || ...</c>;
    /// it is not the base's default.
    ///
    /// <para>
    /// Abstract rather than defaulting to <c>true</c>. A host whose list screen already hides users
    /// outside the caller's scope would otherwise expose them here through a direct call, and a
    /// permissive default fails silently. A host with genuinely no scope rule writes an explicit
    /// <c>true</c>, which is the decision being recorded.
    /// </para>
    /// </remarks>
    protected abstract Task<bool> CanEditUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Whether the current caller may grant every group in <paramref name="groupIds"/> to the user being saved.</summary>
    /// <remarks>
    /// <see cref="UserForEdition.GroupIds"/> is caller-supplied and covered by no other check.
    /// <see cref="CanEditUserAsync"/> answers "may this caller edit this user", which for a caller
    /// editing their own account is trivially <c>true</c>; it says nothing about which groups that
    /// caller may then name in the same request. Without this check, a caller could name their own id
    /// with <c>GroupIds</c> pointing at an administrators group and grant themselves membership in it.
    ///
    /// <para>
    /// Abstract, and called on both creation and update, for the same reason
    /// <see cref="CanEditUserAsync"/> is abstract: a permissive default fails silently, and this
    /// records the decision. A host with genuinely no group-grant rule returns <c>true</c> explicitly.
    /// See the ADR on host-owned row-scope checks on management endpoints for the reasoning behind
    /// choosing an abstract member over a documented convention here.
    /// </para>
    /// </remarks>
    protected abstract Task<bool> CanGrantGroupsAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken);

    /// <summary>Reads <paramref name="userId"/> in its edition shape, or <c>null</c> when no such user exists.</summary>
    /// <remarks>
    /// <see cref="GetAsync"/> promises that a deactivated user is not returned; this base cannot enforce
    /// that from here, so the implementation must treat a deactivated user the same as a missing one and
    /// return <c>null</c> for it. Doing otherwise also contradicts <see cref="IUserNameAvailabilityReader"/>,
    /// which deliberately treats a deactivated user's user name as free: a name that reads as claimable
    /// through the availability endpoint should not still resolve to a visible row here.
    /// </remarks>
    protected abstract Task<TUserForEdition?> ReadForEditionAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates <paramref name="user"/>, returning the outcome of the write and, on a
    /// successful creation, the identifier assigned to the new user.
    /// </summary>
    /// <remarks>
    /// Obligations fall on the host that <see cref="CanEditUserAsync"/>, <see cref="CanGrantGroupsAsync"/>
    /// and the base cannot enforce.
    ///
    /// <para>
    /// <see cref="Guid.Empty"/> as <c>user.Id</c> means insert, never upsert. A host that implements
    /// this hook as a plausible upsert — find by user name, else insert — turns an empty id plus an
    /// existing user name into a write against a row the scope check never ran against, because the
    /// check ran with <see cref="Guid.Empty"/>. Treat an empty id as insert-only. The server, not the
    /// caller, assigns the identifier on creation; a <see cref="Guid.Empty"/> id must never reach the
    /// stored row, and the assigned identifier is what this method returns for <see cref="SaveAsync"/>
    /// to hand back to the caller.
    /// </para>
    /// <para>
    /// An empty <see cref="UserLoginForEdition.Password"/> on update must leave the stored password
    /// unchanged; only a non-empty value sets a new one. The base performs no password handling of any
    /// kind, so this is entirely on the implementation.
    /// </para>
    /// </remarks>
    protected abstract Task<UserSaveResult> WriteForEditionAsync(TUserForEdition user, CancellationToken cancellationToken);
}
