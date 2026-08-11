using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class ScopedPermissionCacheTests
{
    private const string AllowedTag = "CanEditBuilding";
    private const string DeniedTag = "CanDeleteBuilding";

    private IUserPermissionService permissionService;
    private ScopedPermissionCache cache;
    private Guid someUserId;

    [SetUp]
    public void SetUp()
    {
        permissionService = Substitute.For<IUserPermissionService>();
        someUserId = Guid.NewGuid();

        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new List<UserMergedPermission>
            {
                new() { FeatureName = AllowedTag, Access = true },
            }));

        cache = new ScopedPermissionCache(permissionService);
    }

    [Test]
    public async Task SameUserCheckedTwice_WhenHasPermissionAsync_ShouldLoadPermissionsOnce()
    {
        await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);
        await cache.HasPermissionAsync(someUserId, DeniedTag, CancellationToken.None);

        await permissionService.Received(1).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TwoDifferentUsers_WhenHasPermissionAsync_ShouldLoadPermissionsOncePerUser()
    {
        var otherUserId = Guid.NewGuid();

        await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);
        await cache.HasPermissionAsync(otherUserId, AllowedTag, CancellationToken.None);

        await permissionService.Received(1).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
        await permissionService.Received(1).GetPermissionsForUserAsync(otherUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AllowedTag_WhenHasPermissionAsync_ShouldReturnTrue()
    {
        var result = await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    public async Task TagNotGranted_WhenHasPermissionAsync_ShouldReturnFalse()
    {
        var result = await cache.HasPermissionAsync(someUserId, DeniedTag, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    public async Task TagPresentButNotAllowed_WhenHasPermissionAsync_ShouldReturnFalse()
    {
        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserMergedPermission> { new() { FeatureName = AllowedTag, Access = false } });

        var result = await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    public async Task LoadFailsThenSucceeds_WhenHasPermissionAsync_ShouldNotCacheTheFailure()
    {
        permissionService.GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("transient database fault"),
                _ => Task.FromResult(new List<UserMergedPermission>
                {
                    new() { FeatureName = AllowedTag, Access = true },
                }));

        var firstAttempt = async () => await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);
        await firstAttempt.Should().ThrowAsync<InvalidOperationException>();

        var result = await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        result.Should().BeTrue("a failed load must not be cached, so the retry must reach the service");
        await permissionService.Received(2).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SameUserInTwoScopes_WhenHasPermissionAsync_ShouldLoadPermissionsOncePerScope()
    {
        var services = new ServiceCollection();
        services.AddSingleton(permissionService);
        services.AddScoped<ScopedPermissionCache>();
        using var provider = services.BuildServiceProvider();

        using (var firstScope = provider.CreateScope())
            await firstScope.ServiceProvider.GetRequiredService<ScopedPermissionCache>()
                .HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        using (var secondScope = provider.CreateScope())
            await secondScope.ServiceProvider.GetRequiredService<ScopedPermissionCache>()
                .HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        await permissionService.Received(2).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
    }
}
