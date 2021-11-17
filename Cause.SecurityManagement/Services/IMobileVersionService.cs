namespace Cause.SecurityManagement.Services
{
    public interface IMobileVersionService
    {
        bool IsMobileVersionLatest(string mobileVersion);
        bool IsMobileVersionValid(string mobileVersion);
    }
}