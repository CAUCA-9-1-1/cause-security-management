using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Core.Repositories
{
    public class GroupPermissionRepository<TUser> : IGroupPermissionRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public GroupPermissionRepository(
            IScopedDbContextProvider<TUser> contextProvider)
        {
            this.context = contextProvider.GetContext();
        }
        public IQueryable<GroupPermission> GetForGroup(Guid groupId)
        {
            return context.GroupPermissions
                .AsNoTracking()
                .Where(gp => gp.IdGroup == groupId);
        }

        public IQueryable<GroupPermission> GetForUser(Guid userId)
        {
            return
                from userGroup in context.UserGroups
                where userGroup.IdUser == userId
                from groupPermission in userGroup.Group.Permissions
                select groupPermission;
        }

        public Task<List<UserMergedPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.GroupPermissions.AsNoTracking()
                .Where(groupPermission => context.UserGroups
                    .Any(userGroup => userGroup.IdUser == userId && userGroup.IdGroup == groupPermission.IdGroup))
                .Select(groupPermission => new UserMergedPermission { Access = groupPermission.IsAllowed, FeatureName = groupPermission.Permission.Tag })
                .ToListAsync(cancellationToken);
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
        public Task SaveChangesAsync()
        {
            return context.SaveChangesAsync();
        }
    }
}
