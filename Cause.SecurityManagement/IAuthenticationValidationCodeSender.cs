using Cause.SecurityManagement.Models;
using System.Threading.Tasks;

namespace Cause.SecurityManagement
{
    public interface IAuthenticationValidationCodeSender<TUser>
        where TUser : User, new()
    {
        Task SendCodeAsync(TUser user, string code);
    }
}