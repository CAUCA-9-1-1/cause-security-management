using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects;

public record UserPassword
{
    public Guid Id { get; init; }
    public string CurrentPassword { get; init; }
    public string Password { get; init; }
    public string PasswordConfirmation { get; init; }
}

public record PasswordChangeRequest
{
    public string NewPassword { get; init; }
}

public record AccountRecoveryRequest
{
    public string Email { get; init; }
}

public record AccountRecoveryValidationRequest
{
    public string Email { get; init; }
    public string ValidationCode { get; init; }
}