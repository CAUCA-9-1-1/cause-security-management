namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// The effective state of one module permission for one user.
    /// </summary>
    public enum PermissionStatus
    {
        /// <summary>The permission is assigned nowhere — neither through a group nor as a user override.</summary>
        Undefined = 0,

        /// <summary>Every assignment allows it.</summary>
        Allowed = 1,

        /// <summary>At least one assignment denies it. A denial outranks any allowance.</summary>
        Denied = 2,
    }
}
