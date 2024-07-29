using System;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Services;

public interface IEntityAuthenticator
{
    Task<LoginResult> LoginAsync(string userName, string password);
    Task<BaseToken> GenerateRecoveryTokenAsync(Guid entityId);
    Task<LoginResult> ValidateMultiFactorCodeAsync(ValidationInformation validationInformation);
    Task SendNewCodeAsync();
    bool IsLoggedIn(string refreshToken);
}