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

        public UserRepository(ISecurityContext<TUser> context)
        {
            this.context = context;
        }

        public IQueryable<TUser> GetActiveUsers()
        {
            return context.Users
                .Where(u => u.IsActive)
                .Include(u => u.Groups)
                .Include(u => u.Permissions);
        }

        public TUser GetEntityById(Guid idUser)
        {
            return context.Users
                    .SingleOrDefault(user => user.Id == idUser && user.IsActive);
        }

        public TUser GetEntityWithTemporaryPassword(string userName, string password)
        {
            return context.Users
                .FirstOrDefault(user => user.UserName == userName && user.Password == password && user.PasswordMustBeResetAfterLogin && user.IsActive);
        }

        public TUser GetEntity(string userName, string password)
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

        public bool HasToken(Guid entityId, string refreshToken)
        {
            return context.UserTokens
                .Any(t => t.IdUser == entityId && t.RefreshToken == refreshToken);
        }

        public TUser GetEntityByUsername(string userName)
        {
            return context.Users
                .FirstOrDefault(user => (user.UserName == userName || user.Email == userName) && user.IsActive);
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

        public async Task RemoveExistingTokenAsync(Guid entityId, string issuer)
        {
            await context.UserTokens
                .Where(userToken => userToken.IdUser == entityId && (userToken.ForIssuer == issuer || string.IsNullOrEmpty(userToken.ForIssuer)))
                .ExecuteDeleteAsync();
        }
    }
}