using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthenticationService
    {
        (UserToken token, User user) Login(string userName, string password);        
        string RefreshUserToken(string token, string refreshToken);        
        UserToken GenerateUserCreationToken(Guid userId);
        UserToken GenerateUserRecoveryToken(Guid userId);
        (UserToken token, User user) ValidateMultiFactorCode(ValidationInformation validationInformation);
    }
}