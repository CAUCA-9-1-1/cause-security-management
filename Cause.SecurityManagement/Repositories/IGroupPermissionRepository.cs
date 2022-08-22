using System;
using System.Collections.Generic;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Repositories
{
    public interface IGroupPermissionRepository
    {
        List<GroupPermission> GetForGroup(Guid groupId);
        List<GroupPermission> GetForUser(Guid userId);
        GroupPermission Get(Guid groupPermissinId);
        bool Any(Guid groupPermissionId);
        void Add(GroupPermission groupPermission);
        void Remove(GroupPermission groupPermission);
        void Update(GroupPermission groupPermission);
        void SaveChanges();
    }
}
