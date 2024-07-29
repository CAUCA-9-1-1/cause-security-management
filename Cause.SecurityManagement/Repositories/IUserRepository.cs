using Cause.SecurityManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cause.SecurityManagement.Repositories
{
    public interface IUserRepository<TUser> : IAuthenticableEntityRepository<TUser>, IEntityTokenRepository<UserToken>
        where TUser : User, new()
    {
        IQueryable<TUser> GetActiveUsers();
        string GetPassword(Guid userId);
        bool UserNameAlreadyUsed(TUser user);
        bool EmailIsAlreadyInUse(string email, Guid idUserToIgnore);
        TUser Get(Guid userId);
        bool Any(Guid userId);
        void Add(TUser user);
        void Remove(TUser user);
        void Update(TUser user);
        void SaveChanges();
        List<EntityEntry> GetModifieEntities();
    }
}