using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects;

/// <summary>
/// Represents the result of a user save operation (create or update): the <see cref="Outcome"/> and,
/// when the outcome is <see cref="UserSaveOutcome.Done"/> for a creation, the identifier the server
/// assigned to the new user.
/// </summary>
public sealed record UserSaveResult(UserSaveOutcome Outcome, Guid Id)
{
    /// <summary>Builds a result reporting a successful save. Pass the user's identifier, server-assigned on creation.</summary>
    public static UserSaveResult Done(Guid id) => new(UserSaveOutcome.Done, id);

    /// <summary>Builds a result reporting that the user being updated was not found.</summary>
    public static UserSaveResult NotFound() => new(UserSaveOutcome.NotFound, Guid.Empty);

    /// <summary>Builds a result reporting a failed save.</summary>
    public static UserSaveResult Failed() => new(UserSaveOutcome.Failed, Guid.Empty);
}
