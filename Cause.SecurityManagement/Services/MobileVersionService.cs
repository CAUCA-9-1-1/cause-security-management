using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Cause.SecurityManagement.Services
{
    public class MobileVersionService(IOptions<SecurityConfiguration> securityOptions) : IMobileVersionService
    {
        private readonly SecurityConfiguration securityConfiguration = securityOptions.Value;

        public bool IsMobileVersionLatest(string mobileVersion)
        {
            var mobile = new Version(mobileVersion);
            var latestVersion = new Version(securityConfiguration.LatestVersion);

            return mobile.CompareTo(latestVersion) >= 0;
        }

        public bool IsMobileVersionValid(string mobileVersion)
        {
            var mobile = new Version(mobileVersion);
            var minVersion = new Version(securityConfiguration.MinimalVersion);

            return mobile.CompareTo(minVersion) >= 0;
        }
    }
}