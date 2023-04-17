using Cause.SecurityManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cause.SecurityManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cause.SecurityManagement.Repositories
{
    public class UserRepository<TUser> : IUserRepository<TUser>
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public UserRepository(IScopedDbContextProvider<TUser> contextProvider)
        {
            context = contextProvider.GetContext();
        }

        public IQueryable<TUser> GetActiveUsers()
        {
            return context.Users
                .Where(u => u.IsActive)
                .Include(u => u.Groups)
                .Include(u => u.Permissions);
        }

        public TUser GetUserById(Guid idUser)
        {
            return context.Users
                    .SingleOrDefault(user => user.Id == idUser && user.IsActive);
        }

        public TUser GetUserWithTemporaryPassword(string userName, string password)
        {
            return context.Users
                .FirstOrDefault(user => user.UserName == userName && user.Password == password && user.PasswordMustBeResetAfterLogin && user.IsActive);
        }

        public TUser GetUser(string userName, string password)
        {
            return context.Users
                .SingleOrDefault(user => user.UserName == userName && user.Password.ToUpper() == password && user.IsActive);
        }

        public void AddToken(UserToken token)
        {
            context.Add(token);
            context.SaveChanges();
        }

        public UserToken GetToken(Guid idUser, string refreshToken)
        {
            var userToken = context.UserTokens
                .FirstOrDefault(t => t.IdUser == idUser && t.RefreshToken == refreshToken);
            return userToken;
        }

        public string GetPassword(Guid userId)
        {
            return context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Password).First();
        }

        public bool UserNameAlreadyUsed(TUser user)
        {
            return context.Users.Any(c => c.UserName == user.UserName && c.Id != user.Id && c.IsActive);
        }

        public bool EmailIsAlreadyInUse(string email, Guid idUserToIgnore)
        {
            return context.Users
                .Any(c => c.Email.ToLower() == email.ToLower() && c.Id != idUserToIgnore && c.IsActive);
        }

        public TUser Get(Guid userId)
        {
            return context.Users.Find(userId);
        }

        public bool Any(Guid userId)
        {
            return context.Users.AsNoTracking().Any(u => u.Id == userId);
        }
        public void Add(TUser user)
        {
            context.Users.Add(user);
        }

        public void Remove(TUser user)
        {
            context.Users.Remove(user);
        }

        public void Update(TUser user)
        {
            context.Users.Update(user);
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }
        public Task SaveChangesAsync()
        {
            return context.SaveChangesAsync();
        }
        public List<EntityEntry> GetModifieEntities()
        {
            return context.GetModifieObjects();
        }

    }
}