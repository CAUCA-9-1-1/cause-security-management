using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Authentication.MultiFactor
{
    public interface IAuthenticationValidationCodeSender<TUser>
        where TUser : User, new()
    {
        Task SendCodeAsync(TUser user);
        Task SendCodeAsync(TUser user, string code, DateTime expiration, ValidationCodeCommunicationType communicationType = ValidationCodeCommunicationType.Sms);
    }
}