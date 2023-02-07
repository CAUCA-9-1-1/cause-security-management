using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Authentication.MultiFactor
{
    public interface IAuthenticationMultiFactorHandler<TUser>
        where TUser : User, new()
    {
        Task SendValidationCodeWhenNeededAsync(TUser user);
        Task<bool> CodeIsValidAsync(TUser user, string validationCode, ValidationCodeType type);
        Task SendNewValidationCodeAsync(TUser user);
    }
}