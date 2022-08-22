using Cause.SecurityManagement.Models;
using System;
using System.Linq;
using Cause.SecurityManagement.Services;

namespace Cause.SecurityManagement.Repositories
{
    public class ExternalSystemRepository<TUser> : IExternalSystemRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public ExternalSystemRepository(IScopedDbContextProvider<TUser> contextProvider)
        {
            this.context = contextProvider.GetContext();
        }

        public void AddToken(ExternalSystemToken token)
        {
            context.Add(token);
            SaveChanges();
        }

        public ExternalSystem GetByApiKey(string apiKey)
        {
            return context.ExternalSystems
                .SingleOrDefault(externalSystem => externalSystem.ApiKey == apiKey && externalSystem.IsActive);
        }

        public ExternalSystem GetByCertificateSubject(string certificateSubject)
        {
            return context.ExternalSystems
                .SingleOrDefault(externalSystem => externalSystem.CertificateSubjectDn == certificateSubject && externalSystem.IsActive);
        }

        public ExternalSystem GetById(Guid idExternalSystem)
        {
            return context.ExternalSystems
                .FirstOrDefault(system => system.Id == idExternalSystem && system.IsActive);
        }

        public ExternalSystemToken GetCurrentToken(Guid idExternalSystem, string refreshToken)
        {
            return context.ExternalSystemTokens
                .FirstOrDefault(t => t.IdExternalSystem == idExternalSystem && t.RefreshToken == refreshToken);
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }
    }
}