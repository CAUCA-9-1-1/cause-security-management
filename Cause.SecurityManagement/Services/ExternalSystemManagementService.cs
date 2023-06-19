using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Interfaces.Services;
using Cause.SecurityManagement.Interfaces.Repositories;
using System;

namespace Cause.SecurityManagement.Services
{
	public class ExternalSystemManagementService : IExternalSystemManagementService
	{
        private readonly IExternalSystemRepository externalSystemRepository;

        public ExternalSystemManagementService(IExternalSystemRepository externalSystemRepository)
		{
            this.externalSystemRepository = externalSystemRepository;
        }

        public ExternalSystem GetById(Guid externalSystemId)
        {
            return externalSystemRepository.GetById(externalSystemId);
        }

		public bool Update(ExternalSystem externalSystem)
		{
			if (externalSystemRepository.NameAlreadyUsed(externalSystem))
				return false;

            if (externalSystemRepository.Any(externalSystem.Id))
                externalSystemRepository.Update(externalSystem);
            else
                externalSystemRepository.Add(externalSystem);

            externalSystemRepository.SaveChanges();
			return true;
		}
    }
}