using System;
using System.Linq;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Repositories
{
    public interface IGroupPermissionRepository
    {
        IQueryable<GroupPermission> GetForGroup(Guid groupId);
        IQueryable<GroupPermission> GetForUser(Guid userId);
        GroupPermission Get(Guid groupPermissinId);
        bool Any(Guid groupPermissionId);
        void Add(GroupPermission groupPermission);
        void Remove(GroupPermission groupPermission);
        void Update(GroupPermission groupPermission);
        void SaveChanges();
    }
}
