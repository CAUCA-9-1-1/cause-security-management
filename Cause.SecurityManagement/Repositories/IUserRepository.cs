using Cause.SecurityManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Repositories
{
    public interface IUserRepository<TUser>
        where TUser : User, new()
    {
        IQueryable<TUser> GetActiveUsers();
        TUser GetUserById(Guid idUser);
        TUser GetUserWithTemporaryPassword(string userName, string password);
        TUser GetUser(string userName, string password);
        void AddToken(UserToken token);
        UserToken GetToken(Guid idUser, string refreshToken);
        string GetPassword(Guid userId);
        bool UserNameAlreadyUsed(TUser user);
        bool EmailIsAlreadyInUse(string email, Guid idUserToIgnore);
        TUser Get(Guid userId);
        bool Any(Guid userId);
        void Add(TUser user);
        void Remove(TUser user);
        void Update(TUser user);
        void SaveChanges();
        Task SaveChangesAsync();
        List<EntityEntry> GetModifieEntities();
        Task RemoveExistingTokenAsync(Guid userId, string issuer);
    }
}