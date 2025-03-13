using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using Semver;
using Cause.SecurityManagement.Services;

namespace Cause.SecurityManagement.VersionManagement;

public class MobileVersionValidator(IOptions<SecurityConfiguration> securityOptions) : IMobileVersionValidator
{
    private readonly SecurityConfiguration securityConfiguration = securityOptions.Value;

    public bool IsMobileVersionLatest(string mobileVersion)
    {
        var currentVersion = SemVersion.Parse(mobileVersion);
        var latestVersion = SemVersion.Parse(securityConfiguration.LatestVersion);

        return currentVersion.IsNewerOrEqualTo(latestVersion);
    }

    public bool IsMobileVersionValid(string mobileVersion)
    {
        var currentVersion = SemVersion.Parse(mobileVersion);
        var minVersion = SemVersion.Parse(securityConfiguration.MinimalVersion);

        return currentVersion.IsNewerOrEqualTo(minVersion);
    }
}