namespace Cause.SecurityManagement
{
    public interface IAuthenticationValidationCodeSender
    {
        void SendCode(string email, string code);
    }
}