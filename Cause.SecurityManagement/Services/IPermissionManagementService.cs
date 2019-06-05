using Cause.SecurityManagement.Models;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Services
{
    public interface IPermissionManagementService
    {
        List<ModulePermission> GetPermissions();
    }
}