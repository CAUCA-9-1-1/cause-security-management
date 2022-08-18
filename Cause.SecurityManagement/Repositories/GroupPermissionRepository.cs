using System;
using System.Linq;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Repositories
{
    public class GroupPermissionRepository<TUser> : IGroupPermissionRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> securityContext;

        public GroupPermissionRepository(
            ISecurityContext<TUser> securityContext)
        {
            this.securityContext = securityContext;
        }
        public IQueryable<GroupPermission> GetForGroup(Guid groupId)
        {
            return securityContext.GroupPermissions.AsNoTracking().Where(uc => uc.IdGroup == groupId);
        }

        public IQueryable<GroupPermission> GetForUser(Guid userId)
        {
            return
                from userGroup in securityContext.UserGroups
                where userGroup.IdUser == userId
                from groupPermission in userGroup.Group.Permissions
                select groupPermission;
        }

        public GroupPermission Get(Guid groupPermissinId)
        {
            return securityContext.GroupPermissions.Find(groupPermissinId);
        }

        public bool Any(Guid groupPermissionId)
        {
            return securityContext.GroupPermissions.AsNoTracking().Any(g => g.Id == groupPermissionId);
        }

        public void Add(GroupPermission groupPermission)
        {
            securityContext.GroupPermissions.Add(groupPermission);
        }

        public void Remove(GroupPermission groupPermission)
        {
            securityContext.GroupPermissions.Remove(groupPermission);
        }
        public void Update(GroupPermission groupPermission)
        {
            securityContext.GroupPermissions.Update(groupPermission);
        }

        public void SaveChanges()
        {
            securityContext.SaveChanges();
        }
    }
}
