using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Cause.SecurityManagement.Services
{
    public class ExternalSystemAuthenticationService<TUser> 
        : BaseAuthenticationService<TUser>, IExternalSystemAuthenticationService
        where TUser : User, new()
    {
        public ExternalSystemAuthenticationService(
            ISecurityContext<TUser> context,
            IOptions<SecurityConfiguration> securityOptions) 
            : base(context, securityOptions)
        {
        }

        public string RefreshExternalSystemToken(string token, string refreshToken)
        {
            var externalSystemId = GetSidFromExpiredToken(token);
            var externalSystemToken = context.ExternalSystemTokens
                .FirstOrDefault(t => t.IdExternalSystem == externalSystemId && t.RefreshToken == refreshToken);
            var externalSystem = context.ExternalSystems.Find(externalSystemId);

            ThrowExceptionWhenTokenIsNotValid(refreshToken, externalSystemToken);

            var newAccessToken = GenerateAccessToken(externalSystem.Id, externalSystem.Name, SecurityRoles.ExternalSystem);
            // ReSharper disable once PossibleNullReferenceException
            externalSystemToken.AccessToken = newAccessToken;
            context.SaveChanges();

            return newAccessToken;
        }

        public (ExternalSystemToken token, ExternalSystem system) LoginForExternalSystem(string secretApiKey)
        {
            var externalSystemFound = context.ExternalSystems
                .SingleOrDefault(externalSystem => externalSystem.ApiKey == secretApiKey && externalSystem.IsActive);
            if (externalSystemFound != null)
            {
                var accessToken = GenerateAccessToken(externalSystemFound.Id, externalSystemFound.Name, SecurityRoles.ExternalSystem);
                var refreshToken = GenerateRefreshToken();
                var token = new ExternalSystemToken { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = DateTime.Now.AddMinutes(GetRefreshTokenLifeTimeInMinute()), IdExternalSystem = externalSystemFound.Id };
                context.Add(token);
                context.SaveChanges();
                return (token, externalSystemFound);
            }

            return (null, null);
        }
    }
}