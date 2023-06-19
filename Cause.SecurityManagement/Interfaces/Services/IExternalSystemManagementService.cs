using System;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Interfaces.Services
{
    public interface IExternalSystemManagementService
    {
        ExternalSystem GetById(Guid externalSystemId);
        bool Update(ExternalSystem externalSystem);
    }
}
