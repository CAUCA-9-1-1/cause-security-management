using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cause.SecurityManagement.Interfaces;
using Cause.SecurityManagement.Interfaces.Repositories;
using Cause.SecurityManagement.Interfaces.Services;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Repositories
{
    public class UserGroupRepository<TUser> : IUserGroupRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public UserGroupRepository(
            IScopedDbContextProvider<TUser> contextProvider)
        {
            context = contextProvider.GetContext();
        }

        public List<UserGroup> GetForGroup(Guid groupId)
        {
            return context.UserGroups.AsNoTracking().Where(uc => uc.IdGroup == groupId).ToList();
        }
        public List<UserGroup> GetForUser(Guid userId)
        {
            return context.UserGroups.AsNoTracking().Where(uc => uc.IdUser == userId).ToList();
        }

        public UserGroup Get(Guid userGroupId)
        {
            return context.UserGroups.Find(userGroupId);
        }

        public bool Any(Guid userGroupId)
        {
            return context.UserGroups.AsNoTracking().Any(g => g.Id == userGroupId);
        }

        public void Add(UserGroup userGroup)
        {
            context.UserGroups.Add(userGroup);
        }

        public void Remove(UserGroup userGroup)
        {
            context.UserGroups.Remove(userGroup);
        }
        public void Update(UserGroup userGroup)
        {
            context.UserGroups.Update(userGroup);
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
