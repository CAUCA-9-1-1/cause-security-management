namespace Cause.SecurityManagement.Core.Services;

public interface IMobileVersionValidator
{
    bool IsMobileVersionLatest(string mobileVersion);
    bool IsMobileVersionValid(string mobileVersion);
}