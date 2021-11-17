using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Cause.SecurityManagement.Services
{
    public class UserPermissionReader<TUser> : IUserPermissionReader
    where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;
        private readonly ICurrentUserService currentUserService;

        public UserPermissionReader(ISecurityContext<TUser> context, ICurrentUserService currentUserService)
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
    }
}