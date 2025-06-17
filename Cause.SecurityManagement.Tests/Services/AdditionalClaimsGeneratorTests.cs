using System;
using Cause.SecurityManagement.Services;
using AwesomeAssertions;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services;

public class AdditionalClaimsGeneratorTests
{
    [Test]
    public void WithSpecificDeviceId_WhenGeneratingClaims_ShouldContainDeviceIdClaim()
    {
        var deviceId = Guid.NewGuid();

        var result = AdditionalClaimsGenerator.GetCustomClaims(deviceId);

        result.Should().ContainSingle(claim => claim.Type == AdditionalClaimsGenerator.DeviceIdType && claim.Value == deviceId.ToString());
    }

    [Test]
    public void WithoutSpecificDeviceId_WhenGeneratingClaims_ShouldNotContainDeviceIdClaim()
    {
        var result = AdditionalClaimsGenerator.GetCustomClaims(null);

        result.Should().NotContain(claim => claim.Type == AdditionalClaimsGenerator.DeviceIdType);
    }
}