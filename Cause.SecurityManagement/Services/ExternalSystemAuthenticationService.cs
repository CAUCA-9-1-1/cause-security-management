using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Repositories;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public class ExternalSystemAuthenticationService : IExternalSystemAuthenticationService
    {
        private readonly IExternalSystemRepository repository;
        private readonly ITokenReader tokenReader;
        private readonly ITokenGenerator generator;

        public ExternalSystemAuthenticationService(
            IExternalSystemRepository repository,
            ITokenReader tokenReader,
            ITokenGenerator generator)
        {
            this.repository = repository;
            this.tokenReader = tokenReader;
            this.generator = generator;
        }

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
            if (Guid.TryParse(id, out Guid userId))
                return userId;
            return Guid.Empty;
        }
    }
}