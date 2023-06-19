namespace Cause.SecurityManagement.Interfaces.Services
{
    public interface IEmailForUserModificationSender
    {
        void SendEmailForModifiedUser(string emailAddress);
        void SendEmailForModifiedPassword(string password);
    }
}
