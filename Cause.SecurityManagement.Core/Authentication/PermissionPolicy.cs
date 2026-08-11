using System;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Builds and parses the dynamic policy names used by the permission attributes.
/// Use NameFor to gate a minimal-API endpoint: .RequireAuthorization(PermissionPolicy.NameFor(tag, true)).
/// </summary>
public static class PermissionPolicy
{
    public const string Prefix = "Permission:";

    private const string AdministratorOrUserMode = "AdministratorOrUser";
    private const string UserMode = "User";

    /// <summary>
    /// Builds the dynamic policy name for the given tag. Throws ArgumentException when
    /// tag is null, empty, or whitespace-only, since either would parse into a
    /// requirement that can never be satisfied, silently denying everyone.
    /// </summary>
    public static string NameFor(string tag, bool allowAdministrator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        return $"{Prefix}{(allowAdministrator ? AdministratorOrUserMode : UserMode)}:{tag}";
    }

    internal static bool TryParse(string policyName, out string tag, out bool allowAdministrator)
    {
        tag = null;
        allowAdministrator = false;

        if (policyName is null || !policyName.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        var remainder = policyName[Prefix.Length..];
        var separatorIndex = remainder.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == remainder.Length - 1)
            return false;

        var mode = remainder[..separatorIndex];
        if (mode != AdministratorOrUserMode && mode != UserMode)
            return false;

        allowAdministrator = mode == AdministratorOrUserMode;
        tag = remainder[(separatorIndex + 1)..];
        return true;
    }
}
