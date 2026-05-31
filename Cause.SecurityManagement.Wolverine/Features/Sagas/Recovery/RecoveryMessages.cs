using System;
using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Services;
using Wolverine;

namespace Cause.SecurityManagement.Wolverine.Features.Sagas.Recovery;

/// <summary>Starts the account recovery saga.</summary>
public record StartAccountRecovery(string UsernameOrEmail, ValidationCodeCommunicationType CommunicationType = ValidationCodeCommunicationType.Email);

/// <summary>Submits the recovery code and returns a recovery token when valid.</summary>
public record ValidateAccountRecovery(string UsernameOrEmail, string ValidationCode);
