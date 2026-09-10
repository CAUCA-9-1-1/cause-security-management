namespace Cause.SecurityManagement.Models.DataTransferObjects;

/// <summary>
/// Represents the outcome of a user save operation (create or update).
/// </summary>
public enum UserSaveOutcome
{
    /// <summary>
    /// The user was successfully saved.
    /// </summary>
    Done,

    /// <summary>
    /// The user being updated was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The save operation failed for reasons other than the user not being found.
    /// </summary>
    Failed,
}
