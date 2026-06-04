using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public class PermissionCatalogService<TUser>(ISecurityContext<TUser> context)
        : IPermissionCatalogService
        where TUser : User, new()
    {
        public Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return context.ModulePermissions.AsNoTracking()
                .OrderBy(permission => permission.Sequence)
                .Select(permission => new PermissionDto
                {
                    Id = permission.Id,
                    IdModulePermission = permission.Id,
                    Tag = permission.Tag,
                    Name = permission.Name,
                })
                .ToListAsync(cancellationToken);
        }
    }
}
