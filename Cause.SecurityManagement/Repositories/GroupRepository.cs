using System;
using System.Collections.Generic;
using System.Linq;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Repositories
{
    public class GroupRepository<TUser> : IGroupRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public GroupRepository(
            ISecurityContext<TUser> context)
        {
            this.context = context;
        }

        public List<Group> GetActiveGroups()
        {
            return context.Groups
                .Include(g => g.Users)
                .Include(g => g.Permissions)
                .ToList();
        }

        public Group Get(Guid groupId)
        {
            return context.Groups.Find(groupId);
        }

        public bool Any(Guid groupId)
        {
            return context.Groups.AsNoTracking().Any(g => g.Id == groupId);
        }
        public void Add(Group group)
        {
            context.Groups.Add(group);
        }

        public void Remove(Group group)
        {
            context.Groups.Remove(group);
        }

        public void Update(Group group)
        {
            context.Groups.Update(group);
        }

        public bool GroupNameAlreadyUsed(Group group)
        {
            return context.Groups.Any(c => c.Name == group.Name && c.Id != group.Id);
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }

    }
}
