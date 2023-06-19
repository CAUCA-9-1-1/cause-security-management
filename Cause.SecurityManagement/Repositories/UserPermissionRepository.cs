using System;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cause.SecurityManagement.Interfaces.Repositories;
using Cause.SecurityManagement.Interfaces.Services;

namespace Cause.SecurityManagement.Repositories
{
    public class UserPermissionRepository<TUser> : IUserPermissionRepository
    where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public UserPermissionRepository(IScopedDbContextProvider<TUser> contextProvider)
        {
            context = contextProvider.GetContext();
        }

        public async Task<List<AuthenticationUserPermission>> GetUserPermissionsAsync(Guid userId)
        {
            var idGroups = context.UserGroups.AsNoTracking()
                .Where(ug => ug.IdUser == userId)
                .Select(ug => ug.IdGroup).ToList();
            var restrictedPermissions = await context.GroupPermissions.AsNoTracking()
                .Where(g => idGroups.Contains(g.IdGroup) && !g.IsAllowed)
                .Include(g => g.Permission)
                .Select(p => new AuthenticationUserPermission
                {
                    IdModulePermission = p.IdModulePermission,
                    Tag = p.Permission.Tag,
                    IsAllowed = p.IsAllowed,
                })
                .ToListAsync();
            var allowedPermissions = await context.GroupPermissions.AsNoTracking()
                .Where(g => idGroups.Contains(g.IdGroup) && g.IsAllowed && !restrictedPermissions
                                .Select(p => p.IdModulePermission).Contains(g.IdModulePermission))
                .Include(g => g.Permission)
                .Select(p => new AuthenticationUserPermission
                {
                    IdModulePermission = p.IdModulePermission,
                    Tag = p.Permission.Tag,
                    IsAllowed = p.IsAllowed,
                })
                .ToListAsync();

            return restrictedPermissions.Concat(allowedPermissions).Distinct().ToList();
        }

        public List<UserPermission> GetForUser(Guid userId)
        {
            return context.UserPermissions.AsNoTracking()
                .Where(uc => uc.IdUser == userId)
                .Include(uc => uc.Permission)
                .ToList();
        }

        public UserPermission Get(Guid userPermissionId)
        {
            return context.UserPermissions.Find(userPermissionId);
        }

        public bool Any(Guid userPermissionId)
        {
            return context.UserPermissions.AsNoTracking().Any(g => g.Id == userPermissionId);
        }

        public void Add(UserPermission userPermission)
        {
            context.UserPermissions.Add(userPermission);
        }

        public void Remove(UserPermission userPermission)
        {
            context.UserPermissions.Remove(userPermission);
        }
        public void Update(UserPermission userPermission)
        {
            context.UserPermissions.Update(userPermission);
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