using Cause.SecurityManagement.Models;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Repositories
{
    public interface IExternalSystemRepository
    {
        ExternalSystem GetById(Guid idExternalSystem);
        ExternalSystem GetByApiKey(string apiKey);
        ExternalSystem GetByCertificateSubject(string certificateSubject);
        ExternalSystemToken GetCurrentToken(Guid idExternalSystem, string refreshToken);
        void AddToken(ExternalSystemToken token);
        void SaveChanges();
        Task SaveChangesAsync();
    }
}