using Microsoft.AspNetCore.Authorization;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Requires the named permission. When AllowAdministrator is true, a principal in the
/// Administrator role passes without a permission lookup; when false, Administrators are
/// denied like any other non-RegularUser principal.
/// </summary>
public class PermissionRequirement(string tag, bool allowAdministrator) : IAuthorizationRequirement
{
    public string Tag { get; } = tag;
    public bool AllowAdministrator { get; } = allowAdministrator;
}
