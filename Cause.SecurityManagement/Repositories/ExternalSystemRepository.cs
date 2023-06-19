using Cause.SecurityManagement.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Cause.SecurityManagement.Interfaces.Repositories;
using Cause.SecurityManagement.Interfaces.Services;
using Cause.SecurityManagement.Interfaces;

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

        public bool NameAlreadyUsed(ExternalSystem externalSystem)
        {
            return context.ExternalSystems.Any(system => system.Name == externalSystem.Name && system.Id != externalSystem.Id && system.IsActive);
        }

        public bool Any(Guid externalSystemId)
        {
            return context.ExternalSystems.AsNoTracking().Any(externalSystem => externalSystem.Id == externalSystemId);
        }

        public void Add(ExternalSystem externalSystem)
        {
            context.ExternalSystems.Add(externalSystem);
        }

        public void Remove(ExternalSystem externalSystem)
        {
            context.ExternalSystems.Remove(externalSystem);
        }

        public void Update(ExternalSystem externalSystem)
        {
            context.ExternalSystems.Update(externalSystem);
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }
        public Task SaveChangesAsync()
        {
            return context.SaveChangesAsync();
        }
    }
}