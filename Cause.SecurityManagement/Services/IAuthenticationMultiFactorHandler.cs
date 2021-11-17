using Cause.SecurityManagement.Models;
using System;

namespace Cause.SecurityManagement.Services
{
    public interface IAuthenticationMultiFactorHandler<TUser>
        where TUser : User, new()
    {
        void SendValidationCodeWhenNeeded(TUser user);
        bool CodeIsValid(Guid idUser, string validationCode, ValidationCodeType type);
    }
}