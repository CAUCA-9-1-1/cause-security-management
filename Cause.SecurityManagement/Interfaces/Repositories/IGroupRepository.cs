using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Interfaces.Repositories
{
    public interface IGroupRepository
    {
        List<Group> GetActiveGroups();
        Group Get(Guid groupId);
        bool Any(Guid groupId);
        void Add(Group group);
        void Remove(Group group);
        void Update(Group group);
        bool GroupNameAlreadyUsed(Group group);
        void SaveChanges();
        Task SaveChangesAsync();
    }
}
