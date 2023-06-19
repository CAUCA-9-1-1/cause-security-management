using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Cause.SecurityManagement.Interfaces.Repositories;
using Cause.SecurityManagement.Interfaces.Services;

namespace Cause.SecurityManagement.Repositories
{
    public class UserValidationCodeRepository<TUser>
        : IUserValidationCodeRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public UserValidationCodeRepository(IScopedDbContextProvider<TUser> contextProvider)
        {
            context = contextProvider.GetContext();
        }

        public UserValidationCode GetLastCode(Guid idUser)
        {
            var query =
                from code in context.UserValidationCodes.AsNoTracking()
                where code.IdUser == idUser
                orderby code.ExpiresOn descending
                select code;

            return query.FirstOrDefault();
        }

        public void DeleteExistingValidationCode(Guid idUser)
        {
            var existingCode = context.UserValidationCodes
                .Where(code => code.IdUser == idUser).ToList();
            context.UserValidationCodes.RemoveRange(existingCode);
            context.SaveChanges();
        }

        public UserValidationCode GetExistingValidCode(Guid idUser, string validationCode, ValidationCodeType type)
        {
            return context.UserValidationCodes
                .Where(code => code.IdUser == idUser && code.ExpiresOn >= DateTime.Now && code.Code == validationCode && code.Type == type)
                .FirstOrDefault();
        }

        public void SaveNewValidationCode(UserValidationCode code)
        {
            context.UserValidationCodes.Add(code);
            context.SaveChanges();
        }

        public void DeleteCode(UserValidationCode code)
        {
            context.UserValidationCodes.Remove(code);
            context.SaveChanges();
        }
    }
}