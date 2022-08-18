using System;
using System.Linq;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Repositories
{
    public class UserGroupRepository<TUser> : IUserGroupRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> securityContext;

        public UserGroupRepository(
            ISecurityContext<TUser> securityContext)
        {
            this.securityContext = securityContext;
        }

        public IQueryable<UserGroup> GetForGroup(Guid groupId)
        {
            return securityContext.UserGroups.AsNoTracking().Where(uc => uc.IdGroup == groupId);
        }

        public UserGroup Get(Guid userGroupId)
        {
            return securityContext.UserGroups.Find(userGroupId);
        }

        public bool Any(Guid userGroupId)
        {
            return securityContext.UserGroups.AsNoTracking().Any(g => g.Id == userGroupId);
        }

        public void Add(UserGroup userGroup)
        {
            securityContext.UserGroups.Add(userGroup);
        }

        public void Remove(UserGroup userGroup)
        {
            securityContext.UserGroups.Remove(userGroup);
        }
        public void Update(UserGroup userGroup)
        {
            securityContext.UserGroups.Update(userGroup);
        }
        public void SaveChanges()
        {
            securityContext.SaveChanges();
        }

    }
}
