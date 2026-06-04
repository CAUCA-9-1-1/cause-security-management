using Cause.SecurityManagement.Models.ValidationCode;

namespace Cause.SecurityManagement.Wolverine.Features.Sagas.Login;

/// <summary>Starts the login saga. Published by the Logon endpoint when MFA is required.</summary>
public record StartUserLogin(Guid UserId);

/// <summary>Requests a new MFA validation code be sent to the user.</summary>
public record ResendLoginCode(Guid UserId, ValidationCodeCommunicationType CommunicationType = ValidationCodeCommunicationType.Sms);

/// <summary>Submits the MFA code entered by the user.</summary>
public record VerifyLoginCode(Guid UserId, string Code);
