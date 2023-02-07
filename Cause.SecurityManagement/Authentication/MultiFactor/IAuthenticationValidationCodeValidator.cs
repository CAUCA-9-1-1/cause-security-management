using Cause.SecurityManagement.Models;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Authentication.MultiFactor
{
    public interface IAuthenticationValidationCodeValidator<TUser>
        where TUser : User, new()
    {
        Task<bool> CodeIsValidAsync(TUser user, string validationCode);
    }
}