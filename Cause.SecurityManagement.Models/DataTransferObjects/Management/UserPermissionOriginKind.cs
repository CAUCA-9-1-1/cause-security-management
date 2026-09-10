namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// Where one assignment of a module permission comes from.
    /// </summary>
    public enum UserPermissionOriginKind
    {
        /// <summary>Granted through a group the user belongs to.</summary>
        Group = 0,

        /// <summary>Granted directly on the user, overriding nothing — it is merged, not substituted.</summary>
        UserOverride = 1,
    }
}
