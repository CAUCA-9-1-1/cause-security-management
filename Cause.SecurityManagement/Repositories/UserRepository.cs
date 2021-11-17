using Cause.SecurityManagement;
using Cause.SecurityManagement.Models;
using System;
using System.Linq;

namespace Cause.SecurityManagement.Repositories
{
    public class UserRepository<TUser> : IUserRepository<TUser>
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public UserRepository(ISecurityContext<TUser> context)
        {
            this.context = context;
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

        public void SaveChanges()
        {
            context.SaveChanges();
        }
    }
}