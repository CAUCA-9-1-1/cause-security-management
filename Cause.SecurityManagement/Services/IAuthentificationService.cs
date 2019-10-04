using Cause.SecurityManagement.Models;
using System;
using System.Collections.Generic;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthentificationService
    {
        void EnsureAdminIsCreated(string applicationName);
        List<AuthentificationUserPermission> GetActiveUserPermissions(Guid userId);
        bool IsMobileVersionValid(string mobileVersion, string minimalVersion);
        (UserToken token, User user) Login(string userName, string password, string applicationName, string issuer, string secretKey);
        (ExternalSystemToken token, ExternalSystem system) LoginForExternalSystem(string secretApiKey, string applicationName, string issuer, string secretKey);
        string RefreshUserToken(string token, string refreshToken, string applicationName, string issuer, string secretKey);
        string RefreshExternalSystemToken(string token, string refreshToken, string applicationName, string issuer, string secretKey);
    }
}