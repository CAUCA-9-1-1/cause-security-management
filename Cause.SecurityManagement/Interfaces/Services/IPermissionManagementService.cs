using Cause.SecurityManagement.Models;
using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Interfaces.Services
{
    public interface IPermissionManagementService
    {
        List<ModulePermission> GetPermissions();
        bool Add(ModulePermission permission);
        bool Update(ModulePermission permission);
        bool Delete(Guid permissionId);
    }
}