using System;
using System.Linq;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Repositories
{
    public interface IUserGroupRepository
    {
        IQueryable<UserGroup> GetForGroup(Guid groupId);
        IQueryable<UserGroup> GetForUser(Guid userId);
        UserGroup Get(Guid userGroupId);
        bool Any(Guid userGroupId);
        void Add(UserGroup userGroup);
        void Remove(UserGroup userGroup);
        void Update(UserGroup userGroup);
        void SaveChanges();
    }
}
