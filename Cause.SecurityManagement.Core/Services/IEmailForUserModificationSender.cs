namespace Cause.SecurityManagement.Core.Services
{
    public interface IEmailForUserModificationSender
    {
        void SendEmailForModifiedUser(string emailAddress);
        void SendEmailForModifiedPassword(string password);
    }
}
