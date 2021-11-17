using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthenticationService
    {
        void EnsureAdminIsCreated();
        Task<(UserToken token, User user)> LoginAsync(string userName, string password);        
        string RefreshUserToken(string token, string refreshToken);        
        UserToken GenerateUserCreationToken(Guid userId);
        UserToken GenerateUserRecoveryToken(Guid userId);
        (UserToken token, User user) ValidateMultiFactorCode(ValidationInformation validationInformation);
    }
}