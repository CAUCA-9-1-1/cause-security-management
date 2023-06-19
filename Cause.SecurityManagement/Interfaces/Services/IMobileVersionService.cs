namespace Cause.SecurityManagement.Interfaces.Services
{
    public interface IMobileVersionService
    {
        bool IsMobileVersionLatest(string mobileVersion);
        bool IsMobileVersionValid(string mobileVersion);
    }
}