using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthenticationService
    {
	    void SetCurrentUser(Guid userId);
        void EnsureAdminIsCreated();
        List<AuthenticationUserPermission> GetActiveUserPermissions(Guid userId);
        bool IsMobileVersionValid(string mobileVersion);
        (UserToken token, User user) Login(string userName, string password);
        (ExternalSystemToken token, ExternalSystem system) LoginForExternalSystem(string secretApiKey);
        string RefreshUserToken(string token, string refreshToken);
        string RefreshExternalSystemToken(string token, string refreshToken);
    }
}