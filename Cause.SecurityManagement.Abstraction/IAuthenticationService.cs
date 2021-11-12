using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthenticationService
    {
        void EnsureAdminIsCreated();
        List<AuthenticationUserPermission> GetActiveUserPermissions();
        bool IsMobileVersionLatest(string mobileVersion);
        bool IsMobileVersionValid(string mobileVersion);
        (UserToken token, User user) Login(string userName, string password);        
        string RefreshUserToken(string token, string refreshToken);        
        UserToken GenerateUserCreationToken(Guid userId);
        UserToken GenerateUserRecoveryToken(Guid userId);
    }
}