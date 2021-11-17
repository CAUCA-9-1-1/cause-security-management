using System.Threading.Tasks;

namespace Cause.SecurityManagement
{
    public interface IAuthenticationValidationCodeSender
    {
        Task SendCodeAsync(string email, string code);
    }
}