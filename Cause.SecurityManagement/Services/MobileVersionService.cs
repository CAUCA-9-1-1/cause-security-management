using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Cause.SecurityManagement.Services
{
    public class MobileVersionService : IMobileVersionService
    {
        private readonly SecurityConfiguration securityConfiguration;

        public MobileVersionService(IOptions<SecurityConfiguration> securityOptions)
        {
            securityConfiguration = securityOptions.Value;
        }

        public bool IsMobileVersionLatest(string mobileVersion)
        {
            var mobile = new Version(mobileVersion);
            var latestVersion = new Version(securityConfiguration.LatestVersion);

            if (mobile.CompareTo(latestVersion) < 0)
                return false;
            return true;
        }

        public bool IsMobileVersionValid(string mobileVersion)
        {
            var mobile = new Version(mobileVersion);
            var minVersion = new Version(securityConfiguration.MinimalVersion);

            if (mobile.CompareTo(minVersion) < 0)
                return false;
            return true;
        }
    }
}