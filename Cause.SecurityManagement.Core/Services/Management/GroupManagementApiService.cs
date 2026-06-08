using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public class GroupManagementApiService<TUser>(ISecurityContext<TUser> context)
        : IGroupManagementApiService
        where TUser : User, new()
    {
        public async Task<GroupDto> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var group = await context.Groups.AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == groupId, cancellationToken);
            if (group == null)
                return null;

            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                AssignableByAllUsers = group.AssignableByAllUsers,
                Permissions = await GetPermissionsAsync(groupId, cancellationToken),
                Users = await GetMembersAsync(groupId, cancellationToken),
            };
        }

        public async Task<GroupDto> SaveGroupAsync(GroupDto group, CancellationToken cancellationToken = default)
        {
            await SaveGroupDetailsAsync(group, cancellationToken);
            await ReconcilePermissionsAsync(group, cancellationToken);
            await ReconcileMembershipAsync(group, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return await GetGroupAsync(group.Id, cancellationToken);
        }

        public async Task<bool> IsGroupNameAvailableAsync(string name, Guid? excludeGroupId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalizedName = name.ToUpperInvariant();
            return !await context.Groups
                .AsNoTracking()
                .Where(group => group.Name.ToUpper() == normalizedName
                    && (excludeGroupId == null || group.Id != excludeGroupId.Value))
                .AnyAsync(cancellationToken);
        }

        public async Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var group = await context.Groups.FindAsync([groupId], cancellationToken);
            if (group == null)
                return false;

            var permissions = await context.GroupPermissions
                .Where(permission => permission.IdGroup == groupId)
                .ToListAsync(cancellationToken);
            var memberships = await context.UserGroups
                .Where(membership => membership.IdGroup == groupId)
                .ToListAsync(cancellationToken);

            context.GroupPermissions.RemoveRange(permissions);
            context.UserGroups.RemoveRange(memberships);
            context.Groups.Remove(group);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private Task<List<GroupPermissionDto>> GetPermissionsAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return context.GroupPermissions.AsNoTracking()
                .Where(permission => permission.IdGroup == groupId)
                .Select(permission => new GroupPermissionDto
                {
                    Id = permission.Id,
                    IdGroup = permission.IdGroup,
                    IdModulePermission = permission.IdModulePermission,
                    IsAllowed = permission.IsAllowed,
                })
                .ToListAsync(cancellationToken);
        }

        private Task<List<GroupUserDto>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return (
                from membership in context.UserGroups.AsNoTracking()
                where membership.IdGroup == groupId
                join user in context.Users.AsNoTracking() on membership.IdUser equals user.Id
                where user.IsActive
                orderby user.LastName, user.FirstName
                select new GroupUserDto
                {
                    Id = user.Id,
                    FullName = user.FirstName + " " + user.LastName,
                }).ToListAsync(cancellationToken);
        }

        private async Task SaveGroupDetailsAsync(GroupDto group, CancellationToken cancellationToken)
        {
            var existingGroup = await context.Groups.FindAsync([group.Id], cancellationToken);
            if (existingGroup == null)
            {
                context.Groups.Add(new Group
                {
                    Id = group.Id,
                    Name = group.Name,
                    AssignableByAllUsers = group.AssignableByAllUsers,
                });
            }
            else
            {
                existingGroup.Name = group.Name;
                existingGroup.AssignableByAllUsers = group.AssignableByAllUsers;
            }
        }

        private async Task ReconcilePermissionsAsync(GroupDto group, CancellationToken cancellationToken)
        {
            var desiredPermissions = group.Permissions ?? new List<GroupPermissionDto>();
            var existingPermissions = await context.GroupPermissions
                .Where(permission => permission.IdGroup == group.Id)
                .ToListAsync(cancellationToken);

            existingPermissions
                .Where(existing => desiredPermissions.TrueForAll(desired => desired.Id != existing.Id))
                .ToList()
                .ForEach(permission => context.GroupPermissions.Remove(permission));

            foreach (var desired in desiredPermissions)
            {
                var existing = existingPermissions.Find(permission => permission.Id == desired.Id);
                if (existing == null)
                {
                    context.GroupPermissions.Add(new GroupPermission
                    {
                        Id = desired.Id,
                        IdGroup = group.Id,
                        IdModulePermission = desired.IdModulePermission,
                        IsAllowed = desired.IsAllowed,
                    });
                }
                else
                {
                    existing.IdModulePermission = desired.IdModulePermission;
                    existing.IsAllowed = desired.IsAllowed;
                }
            }
        }

        private async Task ReconcileMembershipAsync(GroupDto group, CancellationToken cancellationToken)
        {
            var desiredUserIds = (group.Users ?? new List<GroupUserDto>())
                .Select(user => user.Id)
                .Distinct()
                .ToList();
            var existingMemberships = await context.UserGroups
                .Where(membership => membership.IdGroup == group.Id)
                .ToListAsync(cancellationToken);

            existingMemberships
                .Where(existing => !desiredUserIds.Contains(existing.IdUser))
                .ToList()
                .ForEach(membership => context.UserGroups.Remove(membership));

            var existingUserIds = existingMemberships.Select(membership => membership.IdUser).ToHashSet();
            foreach (var userId in desiredUserIds.Where(userId => !existingUserIds.Contains(userId)))
            {
                context.UserGroups.Add(new UserGroup
                {
                    Id = Guid.NewGuid(),
                    IdGroup = group.Id,
                    IdUser = userId,
                });
            }
        }
    }
}
