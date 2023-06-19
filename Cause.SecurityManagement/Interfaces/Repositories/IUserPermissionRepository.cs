using System;
using Cause.SecurityManagement.Models.DataTransferObjects;
using System.Collections.Generic;
using Cause.SecurityManagement.Models;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Interfaces.Repositories
{
    public interface IUserPermissionRepository
    {
        Task<List<AuthenticationUserPermission>> GetUserPermissionsAsync(Guid userId);
        List<UserPermission> GetForUser(Guid userId);
        UserPermission Get(Guid userPermissionId);
        bool Any(Guid userPermissionId);
        void Add(UserPermission userPermission);
        void Remove(UserPermission userPermission);
        void Update(UserPermission userPermission);
        void SaveChanges();
        Task SaveChangesAsync();
    }
}