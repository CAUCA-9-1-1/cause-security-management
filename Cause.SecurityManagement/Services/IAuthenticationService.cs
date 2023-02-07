using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthenticationService
    {
        Task<(UserToken token, User user)> LoginAsync(string userName, string password);
        string RefreshUserToken(string token, string refreshToken);
        UserToken GenerateUserCreationToken(Guid userId);
        UserToken GenerateUserRecoveryToken(Guid userId);
        Task<(UserToken token, User user)> ValidateMultiFactorCodeAsync(ValidationInformation validationInformation);
        Task SendNewCodeAsync();
        bool MustValidateCode(User user);
    }
}