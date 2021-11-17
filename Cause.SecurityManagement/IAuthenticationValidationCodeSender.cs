using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement
{
    public interface IAuthenticationValidationCodeSender<TUser>
        where TUser : User, new()
    {
        void SendCode(TUser user, string code);
    }
}