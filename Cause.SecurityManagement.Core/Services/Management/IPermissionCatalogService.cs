using System.Collections.Generic;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;

namespace Cause.SecurityManagement.Core.Services.Management
{
    /// <summary>
    /// Exposes the catalog of assignable module permissions for the management UI.
    /// </summary>
    public interface IPermissionCatalogService
    {
        List<PermissionDto> GetPermissions();
    }
}
