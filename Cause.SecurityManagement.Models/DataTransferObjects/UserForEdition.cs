using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects;

public record UserForEdition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UserName { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Password { get; init; }
    public string PasswordConfirmation { get; init; }
    public bool TwoFactorAuthenticatorEnabled { get; init; } = true;
}
