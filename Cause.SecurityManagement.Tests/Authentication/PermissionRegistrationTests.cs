using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class PermissionRegistrationTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildProviderWithPermissionService(Action<IServiceCollection> configure)
    {
        return BuildProvider(services =>
        {
            services.AddScoped(_ => Substitute.For<IUserPermissionService>());
            configure(services);
        });
    }

    [Test]
    public void PermissionRegistrationAfterAuthorization_WhenResolvingThePolicyProvider_ShouldReturnThePermissionProvider()
    {
        using var provider = BuildProvider(services =>
        {
            services.AddAuthorizationForRegularUser();
            services.AddPermissionBasedAuthorization();
        });

        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        policyProvider.Should().BeOfType<PermissionAuthorizationPolicyProvider>();
    }

    [Test]
    public async Task PermissionRegistrationAfterAuthorization_WhenResolvingTheFallbackPolicy_ShouldPreserveTheConfiguredRoleRequirement()
    {
        using var provider = BuildProvider(services =>
        {
            services.AddAuthorizationForRegularUser();
            services.AddPermissionBasedAuthorization();
        });

        var fallbackPolicy = await provider.GetRequiredService<IAuthorizationPolicyProvider>()
            .GetFallbackPolicyAsync();

        fallbackPolicy.Should().NotBeNull(
            "AddAuthorization() must not clear the fallback policy the AddAuthorizationFor* extension configured");
        fallbackPolicy.Requirements.Should().ContainItemsAssignableTo<RolesAuthorizationRequirement>();
    }

    [Test]
    public void PermissionRegistrationBeforeAuthorization_WhenResolvingThePolicyProvider_ShouldReturnThePermissionProvider()
    {
        using var provider = BuildProvider(services =>
        {
            services.AddPermissionBasedAuthorization();
            services.AddAuthorizationForRegularUser();
        });

        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        policyProvider.Should().BeOfType<PermissionAuthorizationPolicyProvider>(
            "AddAuthorizationCore registers the default provider with TryAdd, so registration order must not decide which provider wins");
    }

    [Test]
    public void RegisteredTwice_WhenResolvingTheHandlers_ShouldContainExactlyOnePermissionHandler()
    {
        using var provider = BuildProvider(services =>
        {
            services.AddPermissionBasedAuthorization();
            services.AddPermissionBasedAuthorization();
        });

        var handlers = provider.GetServices<IAuthorizationHandler>();

        handlers.OfType<PermissionAuthorizationHandler>().Should().ContainSingle();
    }

    [Test]
    public void PermissionRegistration_WhenResolvingTheCacheTwiceInOneScope_ShouldReturnTheSameInstance()
    {
        using var provider = BuildProviderWithPermissionService(services => services.AddPermissionBasedAuthorization());
        using var scope = provider.CreateScope();

        var firstResolution = scope.ServiceProvider.GetRequiredService<ScopedPermissionCache>();
        var secondResolution = scope.ServiceProvider.GetRequiredService<ScopedPermissionCache>();

        secondResolution.Should().BeSameAs(firstResolution);
    }

    [Test]
    public void PermissionRegistration_WhenResolvingTheCacheInTwoScopes_ShouldReturnDifferentInstances()
    {
        using var provider = BuildProviderWithPermissionService(services => services.AddPermissionBasedAuthorization());

        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstInstance = firstScope.ServiceProvider.GetRequiredService<ScopedPermissionCache>();
        var secondInstance = secondScope.ServiceProvider.GetRequiredService<ScopedPermissionCache>();

        secondInstance.Should().NotBeSameAs(firstInstance,
            "the cache must be per-request, so a permission revoked between requests is visible to the next request");
    }

    [Test]
    public void PermissionRegistration_WhenResolvingTheHttpContextAccessor_ShouldSucceed()
    {
        using var provider = BuildProvider(services => services.AddPermissionBasedAuthorization());

        var act = () => provider.GetRequiredService<IHttpContextAccessor>();

        act.Should().NotThrow();
    }

    [Test]
    public void PermissionRegistration_WhenResolvingThePolicyProvider_ShouldAllowCachingPolicies()
    {
        using var provider = BuildProvider(services => services.AddPermissionBasedAuthorization());

        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        policyProvider.AllowsCachingPolicies.Should().BeTrue();
    }
}
