using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;

namespace Cause.SecurityManagement.Core.Services.Management
{
    /// <summary>
    /// Exposes the catalog of assignable module permissions for the group management UI.
    /// </summary>
    public interface IPermissionCatalogService
    {
        Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    }
}
