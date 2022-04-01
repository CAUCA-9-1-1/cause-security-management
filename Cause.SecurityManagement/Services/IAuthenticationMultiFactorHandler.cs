using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthenticationMultiFactorHandler<TUser>
        where TUser : User, new()
    {
        Task SendValidationCodeWhenNeededAsync(TUser user);
        bool CodeIsValid(Guid idUser, string validationCode, ValidationCodeType type);
        Task SendNewValidationCodeAsync(TUser idUser);
    }
}