using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public interface IUserAuthenticator
    {
        Task<(UserToken token, User user)> LoginAsync(string userName, string password);
        UserToken GenerateUserCreationToken(Guid userId);
        Task<UserToken> GenerateUserRecoveryTokenAsync(Guid userId);
        Task<(UserToken token, User user)> ValidateMultiFactorCodeAsync(ValidationInformation validationInformation);
        Task SendNewCodeAsync();
        bool MustValidateCode(User user);
        bool IsLoggedIn(string refreshToken);
    }
}