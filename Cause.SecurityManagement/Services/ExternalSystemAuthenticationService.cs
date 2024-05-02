using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Repositories;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public class ExternalSystemAuthenticationService(
        IExternalSystemRepository repository,
        ITokenReader tokenReader,
        ITokenGenerator generator)
        : IExternalSystemAuthenticationService
    {
        public async Task<string> RefreshAccessTokenAsync(string token, string refreshToken)
        {
            var externalSystemId = GetIdFromExpiredToken(token);
            var externalSystemToken = repository.GetCurrentToken(externalSystemId, refreshToken);
            var externalSystem = repository.GetById(externalSystemId);

            tokenReader.ThrowExceptionWhenTokenIsNotValid(refreshToken, externalSystemToken);

            var newAccessToken = generator.GenerateAccessToken(externalSystem.Id.ToString(), externalSystem.Name, SecurityRoles.ExternalSystem);
            // ReSharper disable once PossibleNullReferenceException
            externalSystemToken.AccessToken = newAccessToken;
            await repository.SaveChangesAsync();

            return newAccessToken;
        }

        public (ExternalSystemToken token, ExternalSystem system) Login(string secretApiKey)
        {
            var externalSystemFound = repository.GetByApiKey(secretApiKey);
            if (externalSystemFound != null)
            {
                var accessToken = generator.GenerateAccessToken(externalSystemFound.Id.ToString(), externalSystemFound.Name, SecurityRoles.ExternalSystem);
                var refreshToken = generator.GenerateRefreshToken();
                var token = GenerateExternalSystemToken(accessToken, refreshToken, externalSystemFound);
                repository.AddToken(token);
                return (token, externalSystemFound);
            }

            return (null, null);
        }

        private ExternalSystemToken GenerateExternalSystemToken(string accessToken, string refreshToken, ExternalSystem externalSystemFound)
        {
            return new ExternalSystemToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresOn = DateTime.Now.AddMinutes(generator.GetRefreshTokenLifeTimeInMinute()),
                IdExternalSystem = externalSystemFound.Id,
            };
        }

        private Guid GetIdFromExpiredToken(string token)
        {
            var id = tokenReader.GetSidFromExpiredToken(token);
            return Guid.TryParse(id, out var userId) ? userId : Guid.Empty;
        }
    }
}