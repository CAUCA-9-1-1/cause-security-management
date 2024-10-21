using System;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;

namespace Cause.SecurityManagement.Services;

public interface IEntityAuthenticator
{
    Task<LoginResult> LoginAsync(string userName, string password);
    Task<BaseToken> GenerateRecoveryTokenAsync(Guid entityId);
    Task<LoginResult> ValidateMultiFactorCodeAsync(ValidationInformation validationInformation);
    Task SendNewCodeAsync(ValidationCodeCommunicationType communicationType = ValidationCodeCommunicationType.Sms);
    bool IsLoggedIn(string refreshToken);
}