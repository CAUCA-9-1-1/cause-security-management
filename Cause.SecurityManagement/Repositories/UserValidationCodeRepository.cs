using Cause.SecurityManagement.Models;
using System;
using System.Linq;

namespace Cause.SecurityManagement.Repositories
{
    public class UserValidationCodeRepository<TUser>
        : IUserValidationCodeRepository
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public UserValidationCodeRepository(ISecurityContext<TUser> context)
        {
            this.context = context;
        }

        public void DeleteExistingValidationCode(Guid idUser, ValidationCodeType type)
        {
            var existingCode = context.UserValidationCodes
                .Where(code => code.IdUser == idUser && code.Type == type).ToList();
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

    public interface IUserValidationCodeRepository
    {
        void DeleteExistingValidationCode(Guid idUser, ValidationCodeType type);
        void SaveNewValidationCode(UserValidationCode code);
        UserValidationCode GetExistingValidCode(Guid idUser, string code, ValidationCodeType type);
        void DeleteCode(UserValidationCode code);
    }
}