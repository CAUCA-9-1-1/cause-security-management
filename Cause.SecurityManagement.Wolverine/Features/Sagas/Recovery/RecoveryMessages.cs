using Cause.SecurityManagement.Models.ValidationCode;

namespace Cause.SecurityManagement.Wolverine.Features.Sagas.Recovery;

/// <summary>Starts the account recovery saga.</summary>
public record StartAccountRecovery(string UsernameOrEmail, ValidationCodeCommunicationType CommunicationType = ValidationCodeCommunicationType.Email);

/// <summary>Submits the recovery code and returns a recovery token when valid.</summary>
public record ValidateAccountRecovery(string UsernameOrEmail, string ValidationCode);
