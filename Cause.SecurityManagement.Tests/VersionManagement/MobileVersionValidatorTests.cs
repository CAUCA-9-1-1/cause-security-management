using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.VersionManagement;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.VersionManagement;

[TestFixture]
internal class MobileVersionValidatorTests
{
    private SecurityConfiguration configuration;
    private MobileVersionValidator validator;

    [SetUp]
    public void Setup()
    {
        configuration = new SecurityConfiguration
        {
            MinimalVersion = "1.5.0",
            LatestVersion = "1.5.0"
        };
        validator = new MobileVersionValidator(Options.Create(configuration));
    }

    [TestCase("1.5.0", true)]
    [TestCase("1.5.1", true)]
    [TestCase("1.4.0", false)]
    [TestCase("1.5.0-beta1", false)]
    [TestCase("1.6.0-beta1", true)]
    public void SomeVersion_WhenValidatingIfMobileIsLatest_ShouldCorrectlyParseAndCompare(string mobileVersion, bool isLatest)
    {
        var result = validator.IsMobileVersionLatest(mobileVersion);

        result.Should().Be(isLatest);
    }

    [TestCase("1.5.0", true)]
    [TestCase("1.5.1", true)]
    [TestCase("1.4.0", false)]
    [TestCase("1.5.0-beta1", false)]
    [TestCase("1.6.0-beta1", true)]
    public void SomeVersion_WhenValidatingIfMobileVersionIsReachingMinimalVersion_ShouldCorrectlyParseAndCompare(string mobileVersion, bool isLatest)
    {
        var result = validator.IsMobileVersionValid(mobileVersion);
        result.Should().Be(isLatest);
    }
}
