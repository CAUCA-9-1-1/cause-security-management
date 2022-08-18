using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Cause.SecurityManagement.Repositories
{
    public class GroupRepository<TUser> : IGroupRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> securityContext;

        public GroupRepository(
            ISecurityContext<TUser> securityContext)
        {
            this.securityContext = securityContext;
        }

        public List<Group> GetActiveGroups()
        {
            return securityContext.Groups
                .Include(g => g.Users)
                .Include(g => g.Permissions)
                .ToList();
        }

        public Group Get(Guid groupId)
        {
            return securityContext.Groups.Find(groupId);
        }

        public bool Any(Guid groupId)
        {
            return securityContext.Groups.AsNoTracking().Any(g => g.Id == groupId);
        }
        public void Add(Group group)
        {
            securityContext.Groups.Add(group);
        }

        public void Remove(Group group)
        {
            securityContext.Groups.Remove(group);
        }

        public void Update(Group group)
        {
            securityContext.Groups.Update(group);
        }

        public bool GroupNameAlreadyUsed(Group group)
        {
            return securityContext.Groups.Any(c => c.Name == group.Name && c.Id != group.Id);
        }

        public void SaveChanges()
        {
            securityContext.SaveChanges();
        }

    }
}
