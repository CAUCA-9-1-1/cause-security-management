using Cause.SecurityManagement.VersionManagement;
using FluentAssertions;
using NUnit.Framework;
using Semver;

namespace Cause.SecurityManagement.Tests.VersionManagement;

[TestFixture]
public class SemVersionExtensionsTests
{
    [TestCase("1.0.0", "2.0.0")]
    [TestCase("1.0.0", "1.1.0")]
    [TestCase("1.0.0", "1.0.1")]
    [TestCase("1.0.0-beta1", "1.0.0-beta2")]
    [TestCase("1.0.0-beta1", "1.0.0")]
    [TestCase("1.1.0", "1.1.1-beta1")]
    public void NewerVersion_WhenCheckingIfNewerThan_ShouldReturnTrue(string olderVersion, string newerVersion)
    {
        var result = SemVersion.Parse(newerVersion).IsNewerThan(SemVersion.Parse(olderVersion));

        result.Should().BeTrue();
    }
}
