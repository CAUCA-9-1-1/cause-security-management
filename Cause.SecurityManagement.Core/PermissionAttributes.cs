using Cause.SecurityManagement.Core.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Cause.SecurityManagement.Core;

/// <summary>
/// Administrators pass without a permission lookup. RegularUsers pass only when they hold
/// the named permission. Every other principal is denied. Use this in applications that use
/// Keycloak, where Administrator identifies a Keycloak-authenticated principal.
/// Stacking multiple permission attributes on the same endpoint ANDs the requirements,
/// since AuthorizeAttribute allows multiple and is inherited: two tags on one action
/// require both permissions, and a controller-level attribute combines with an
/// action-level one. Mixing this attribute with UserWithPermissionAttribute on the same
/// endpoint yields requirements with conflicting AllowAdministrator, denying Administrators.
/// AllowAnonymous anywhere on the endpoint, including inherited from a base controller,
/// disables this gate entirely.
/// </summary>
public class AdministratorOrUserWithPermissionAttribute : AuthorizeAttribute
{
    public AdministratorOrUserWithPermissionAttribute(string tag)
        => Policy = PermissionPolicy.NameFor(tag, allowAdministrator: true);
}

/// <summary>
/// RegularUsers pass only when they hold the named permission. Every other principal is
/// denied, including Administrators. Intended for applications with no Administrator
/// principals. Warning: Keycloak-authenticated principals hold Administrator and not
/// RegularUser, so this attribute denies all of them.
/// Stacking multiple permission attributes on the same endpoint ANDs the requirements,
/// since AuthorizeAttribute allows multiple and is inherited: two tags on one action
/// require both permissions, and a controller-level attribute combines with an
/// action-level one. Mixing this attribute with AdministratorOrUserWithPermissionAttribute
/// on the same endpoint yields requirements with conflicting AllowAdministrator, denying
/// Administrators. AllowAnonymous anywhere on the endpoint, including inherited from a
/// base controller, disables this gate entirely.
/// </summary>
public class UserWithPermissionAttribute : AuthorizeAttribute
{
    public UserWithPermissionAttribute(string tag)
        => Policy = PermissionPolicy.NameFor(tag, allowAdministrator: false);
}
