namespace Cause.SecurityManagement.Services
{
    public interface IEmailForUserModificationSender
    {
        void SendEmailForModifiedUser(string emailAddress);
        void SendEmailForModifiedPassword(string password);
    }
}
