using System;
using System.Collections.Generic;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Interfaces.Services
{
    public interface IUserPermissionService
    {
        bool HasPermission(Guid userId, string permissionTag);
        List<UserMergedPermission> GetPermissionsForUser(Guid userId);
    }
}
