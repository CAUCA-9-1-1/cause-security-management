using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Repositories
{
    public class GroupPermissionRepository<TUser> : IGroupPermissionRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public GroupPermissionRepository(
            ISecurityContext<TUser> context)
        {
            this.context = context;
        }
        public List<GroupPermission> GetForGroup(Guid groupId)
        {
            return context.GroupPermissions.AsNoTracking().Where(uc => uc.IdGroup == groupId).ToList();
        }

        public List<GroupPermission> GetForUser(Guid userId)
        {
            var query =
                from userGroup in context.UserGroups
                where userGroup.IdUser == userId
                from groupPermission in userGroup.Group.Permissions
                select groupPermission;
            return query.ToList();
        }

        public GroupPermission Get(Guid groupPermissinId)
        {
            return context.GroupPermissions.Find(groupPermissinId);
        }

        public bool Any(Guid groupPermissionId)
        {
            return context.GroupPermissions.AsNoTracking().Any(g => g.Id == groupPermissionId);
        }

        public void Add(GroupPermission groupPermission)
        {
            context.GroupPermissions.Add(groupPermission);
        }

        public void Remove(GroupPermission groupPermission)
        {
            context.GroupPermissions.Remove(groupPermission);
        }
        public void Update(GroupPermission groupPermission)
        {
            context.GroupPermissions.Update(groupPermission);
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }
    }
}
