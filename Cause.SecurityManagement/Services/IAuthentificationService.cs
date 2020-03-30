using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthentificationService
    {
	    void SetCurrentUser(Guid userId);
        void EnsureAdminIsCreated();
        List<AuthentificationUserPermission> GetActiveUserPermissions(Guid userId);
        bool IsMobileVersionValid(string mobileVersion);
        (UserToken token, User user) Login(string userName, string password);
        (ExternalSystemToken token, ExternalSystem system) LoginForExternalSystem(string secretApiKey);
        string RefreshUserToken(string token, string refreshToken);
        string RefreshExternalSystemToken(string token, string refreshToken);
    }
}