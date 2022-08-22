using System;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System.Collections.Generic;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Repositories
{
    public interface IUserPermissionRepository
    {
        List<AuthenticationUserPermission> GetActiveUserPermissions();
        List<UserPermission> GetForUser(Guid userId);
        UserPermission Get(Guid groupPermissinId);
        bool Any(Guid groupPermissionId);
        void Add(UserPermission groupPermission);
        void Remove(UserPermission groupPermission);
        void Update(UserPermission groupPermission);
        void SaveChanges();

    }
}