using Cause.SecurityManagement.Models;
using System;

namespace Cause.SecurityManagement.Repositories
{
    public interface IUserValidationCodeRepository
    {
        void DeleteExistingValidationCode(Guid idUser);
        void SaveNewValidationCode(UserValidationCode code);
        UserValidationCode GetExistingValidCode(Guid idUser, string code, ValidationCodeType type);
        UserValidationCode GetLastCode(Guid idUser);
        void DeleteCode(UserValidationCode code);
    }
}