using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Interfaces.Repositories
{
    public interface IUserGroupRepository
    {
        List<UserGroup> GetForGroup(Guid groupId);
        List<UserGroup> GetForUser(Guid userId);
        UserGroup Get(Guid userGroupId);
        bool Any(Guid userGroupId);
        void Add(UserGroup userGroup);
        void Remove(UserGroup userGroup);
        void Update(UserGroup userGroup);
        void SaveChanges();
        Task SaveChangesAsync();
    }
}
