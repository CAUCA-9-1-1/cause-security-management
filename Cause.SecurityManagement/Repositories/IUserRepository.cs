using Cause.SecurityManagement.Models;
using System;

namespace Cause.SecurityManagement.Repositories
{
    public interface IUserRepository<TUser>
        where TUser : User, new()
    {
        TUser GetUserById(Guid idUser);
        TUser GetUserWithTemporaryPassword(string userName, string password);
        TUser GetUser(string userName, string password);
        void AddToken(UserToken token);
        UserToken GetToken(Guid idUser, string refreshToken);
        void SaveChanges();
    }
}