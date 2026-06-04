using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public class GroupManagementApiService<TUser>(ISecurityContext<TUser> context)
        : IGroupManagementApiService
        where TUser : User, new()
    {
        public GroupDto GetGroup(Guid groupId)
        {
            var group = context.Groups.AsNoTracking().FirstOrDefault(g => g.Id == groupId);
            if (group == null)
                return null;

            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                AssignableByAllUsers = group.AssignableByAllUsers,
                Permissions = GetPermissions(groupId),
                Users = GetMembers(groupId),
            };
        }

        public GroupDto SaveGroup(GroupDto group)
        {
            SaveGroupDetails(group);
            ReconcilePermissions(group);
            ReconcileMembership(group);
            context.SaveChanges();
            return GetGroup(group.Id);
        }

        public bool DeleteGroup(Guid groupId)
        {
            var group = context.Groups.Find(groupId);
            if (group == null)
                return false;

            context.GroupPermissions.RemoveRange(context.GroupPermissions.Where(p => p.IdGroup == groupId));
            context.UserGroups.RemoveRange(context.UserGroups.Where(u => u.IdGroup == groupId));
            context.Groups.Remove(group);
            context.SaveChanges();
            return true;
        }

        public List<UserForGroupDto> GetGroupUsers(Guid groupId)
        {
            return (
                from membership in context.UserGroups.AsNoTracking()
                where membership.IdGroup == groupId
                join user in context.Users.AsNoTracking() on membership.IdUser equals user.Id
                orderby user.LastName, user.FirstName
                select new UserForGroupDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                }).ToList();
        }

        public UserSearchResultDto SearchUsers(UserSearchRequestDto request)
        {
            var query = context.Users.AsNoTracking().Where(user => user.IsActive);

            var excludedUserIds = request.ExcludedUserIds ?? new List<Guid>();
            if (excludedUserIds.Count > 0)
                query = query.Where(user => !excludedUserIds.Contains(user.Id));

            var term = (request.Query ?? string.Empty).Trim().ToLower();
            if (term.Length > 0)
                query = query.Where(user =>
                    (user.FirstName != null && user.FirstName.ToLower().Contains(term))
                    || (user.LastName != null && user.LastName.ToLower().Contains(term)));

            var totalCount = query.Count();

            var pagedQuery = query
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .Skip(request.Skip);
            if (request.Top > 0)
                pagedQuery = pagedQuery.Take(request.Top);

            var items = pagedQuery
                .Select(user => new UserForGroupDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                })
                .ToList();

            return new UserSearchResultDto { Items = items, TotalCount = totalCount };
        }

        private List<GroupPermissionDto> GetPermissions(Guid groupId)
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
                .ToList();
        }

        private List<GroupUserDto> GetMembers(Guid groupId)
        {
            return (
                from membership in context.UserGroups.AsNoTracking()
                where membership.IdGroup == groupId
                join user in context.Users.AsNoTracking() on membership.IdUser equals user.Id
                orderby user.LastName, user.FirstName
                select new GroupUserDto
                {
                    Id = user.Id,
                    FullName = user.FirstName + " " + user.LastName,
                }).ToList();
        }

        private void SaveGroupDetails(GroupDto group)
        {
            var existingGroup = context.Groups.Find(group.Id);
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

        private void ReconcilePermissions(GroupDto group)
        {
            var desiredPermissions = group.Permissions ?? new List<GroupPermissionDto>();
            var existingPermissions = context.GroupPermissions
                .Where(permission => permission.IdGroup == group.Id)
                .ToList();

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

        private void ReconcileMembership(GroupDto group)
        {
            var desiredUserIds = (group.Users ?? new List<GroupUserDto>())
                .Select(user => user.Id)
                .Distinct()
                .ToList();
            var existingMemberships = context.UserGroups
                .Where(membership => membership.IdGroup == group.Id)
                .ToList();

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
