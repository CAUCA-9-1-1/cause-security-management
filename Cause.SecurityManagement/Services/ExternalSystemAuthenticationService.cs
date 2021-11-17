using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Repositories;
using System;

namespace Cause.SecurityManagement.Services
{
    public class ExternalSystemAuthenticationService<TUser> 
        : IExternalSystemAuthenticationService
        where TUser : User, new()
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

        public string RefreshAccessToken(string token, string refreshToken)
        {
            var externalSystemId = tokenReader.GetSidFromExpiredToken(token);
            var externalSystemToken = repository.GetCurrentToken(externalSystemId, refreshToken);
            var externalSystem = repository.GetById(externalSystemId);

            tokenReader.ThrowExceptionWhenTokenIsNotValid(refreshToken, externalSystemToken);

            var newAccessToken = generator.GenerateAccessToken(externalSystem.Id, externalSystem.Name, SecurityRoles.ExternalSystem);
            // ReSharper disable once PossibleNullReferenceException
            externalSystemToken.AccessToken = newAccessToken;
            repository.SaveChanges();

            return newAccessToken;
        }

        public (ExternalSystemToken token, ExternalSystem system) Login(string secretApiKey)
        {
            var externalSystemFound = repository.GetByApiKey(secretApiKey);
            if (externalSystemFound != null)
            {
                var accessToken = generator.GenerateAccessToken(externalSystemFound.Id, externalSystemFound.Name, SecurityRoles.ExternalSystem);
                var refreshToken = generator.GenerateRefreshToken();
                var token = new ExternalSystemToken { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresOn = DateTime.Now.AddMinutes(generator.GetRefreshTokenLifeTimeInMinute()), IdExternalSystem = externalSystemFound.Id };
                repository.AddToken(token);
                return (token, externalSystemFound);
            }

            return (null, null);
        }
    }
}