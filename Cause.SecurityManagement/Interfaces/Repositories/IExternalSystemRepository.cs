using Cause.SecurityManagement.Models;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Interfaces.Repositories
{
    public interface IExternalSystemRepository
    {
        ExternalSystem GetById(Guid idExternalSystem);
        ExternalSystem GetByApiKey(string apiKey);
        ExternalSystem GetByCertificateSubject(string certificateSubject);
        ExternalSystemToken GetCurrentToken(Guid idExternalSystem, string refreshToken);
        void AddToken(ExternalSystemToken token);
        bool NameAlreadyUsed(ExternalSystem externalSystem);
        bool Any(Guid externalSystemId);
        void Add(ExternalSystem externalSystem);
        void Remove(ExternalSystem externalSystem);
        void Update(ExternalSystem externalSystem);
        void SaveChanges();
        Task SaveChangesAsync();
    }
}