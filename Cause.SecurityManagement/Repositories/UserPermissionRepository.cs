using System;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Cause.SecurityManagement.Repositories
{
    public class UserPermissionRepository<TUser> : IUserPermissionRepository
    where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;
        private readonly ICurrentUserService currentUserService;

        public UserPermissionRepository(
            ISecurityContext<TUser> context,
            ICurrentUserService currentUserService)
        {
            this.context = context;
            this.currentUserService = currentUserService;
        }

        public List<AuthenticationUserPermission> GetActiveUserPermissions()
        {
            var idGroups = context.UserGroups
                .Where(ug => ug.IdUser == currentUserService.GetUserId())
                .Select(ug => ug.IdGroup).ToList();
            var restrictedPermissions = context.GroupPermissions
                .Where(g => idGroups.Contains(g.IdGroup) && g.IsAllowed == false)
                .Include(g => g.Permission)
                .Select(p => new AuthenticationUserPermission
                {
                    IdModulePermission = p.IdModulePermission,
                    Tag = p.Permission.Tag,
                    IsAllowed = p.IsAllowed,
                })
                .ToList();
            var allowedPermissions = context.GroupPermissions
                .Where(g => idGroups.Contains(g.IdGroup) && g.IsAllowed && !restrictedPermissions
                                .Select(p => p.IdModulePermission).Contains(g.IdModulePermission))
                .Include(g => g.Permission)
                .Select(p => new AuthenticationUserPermission
                {
                    IdModulePermission = p.IdModulePermission,
                    Tag = p.Permission.Tag,
                    IsAllowed = p.IsAllowed,
                })
                .ToList();

            return restrictedPermissions.Concat(allowedPermissions).Distinct().ToList();
        }

        public IQueryable<UserPermission> GetForUser(Guid userId)
        {
            return context.UserPermissions.AsNoTracking().Where(uc => uc.IdUser == userId);
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
    }
}