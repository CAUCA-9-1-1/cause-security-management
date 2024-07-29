using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Authentication.MultiFactor
{
    public interface IAuthenticationMultiFactorHandler<in T>
        where T : IAuthenticableEntity
    {
        Task SendValidationCodeWhenNeededAsync(T entity);
        Task<bool> CodeIsValidAsync(T entity, string validationCode, ValidationCodeType type);
        Task SendNewValidationCodeAsync(T entity);
    }
}