using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Models.DataTransferObjects;

/// <summary>
/// Represents the login credentials for a user during creation or edition.
/// </summary>
public record UserLoginForEdition
{
    /// <summary>
    /// Gets the user's login name.
    /// </summary>
    public string UserName { get; init; } = "";

    /// <summary>
    /// Gets the user's password. On creation, this is required and represents the new password.
    /// On update, if provided with a non-empty value, it sets a new password; if empty, the stored password is left unchanged.
    /// This is the wire contract; the base controller performs no password handling, so honouring it is
    /// the host's obligation in its <c>WriteForEditionAsync</c> implementation.
    /// </summary>
    public string Password { get; init; }

    /// <summary>
    /// Gets a value indicating whether two-factor authentication is enabled for this user. Defaults to true.
    /// </summary>
    public bool TwoFactorAuthenticatorEnabled { get; init; } = true;
}

/// <summary>
/// Represents the personal information for a user during creation or edition.
/// </summary>
public record UserPersonalInformationForEdition
{
    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    public string FirstName { get; init; } = "";

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    public string LastName { get; init; } = "";

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string Email { get; init; } = "";

    /// <summary>
    /// Gets the user's phone number. This property exists on the edition transfer object as a wire contract
    /// for client applications to submit, but is intentionally not persisted to the <c>User</c> entity.
    /// Each consuming application maintains its own phone number property under its own column names
    /// and is responsible for mapping this value from its respective storage when creating or updating users.
    /// </summary>
    public string PhoneNumber { get; init; } = "";
}

/// <summary>
/// Represents the complete user data structure for creation and edition operations.
/// </summary>
public record UserForEdition
{
    /// <summary>
    /// Gets the unique identifier for the user. On creation, this should be set to <see cref="Guid.Empty"/>;
    /// the server generates a new identifier. On update, this must be set to the existing user's identifier.
    /// Using a default non-empty value on creation would make the operation indistinguishable
    /// from an update operation on the wire contract. This is the wire contract; the base controller
    /// does not generate identifiers itself, so honouring it is the host's obligation in its
    /// <c>WriteForEditionAsync</c> implementation, which is also what the save endpoint returns the
    /// generated identifier from on a successful creation.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the login credentials for the user.
    /// </summary>
    public UserLoginForEdition Login { get; init; } = new();

    /// <summary>
    /// Gets the personal information for the user.
    /// </summary>
    public UserPersonalInformationForEdition PersonalInformation { get; init; } = new();

    /// <summary>
    /// Gets the collection of group identifiers this user belongs to.
    /// </summary>
    public List<Guid> GroupIds { get; init; } = [];
}
