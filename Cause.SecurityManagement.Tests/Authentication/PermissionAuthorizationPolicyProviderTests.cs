using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class PermissionAuthorizationPolicyProviderTests
{
    private const string SomeTag = "CanEditBuilding";

    private AuthorizationOptions options;
    private PermissionAuthorizationPolicyProvider provider;

    [SetUp]
    public void SetUp()
    {
        options = new AuthorizationOptions();
        options.AddPolicy("ExistingPolicy", policy => policy.RequireAssertion(_ => true));
        provider = new PermissionAuthorizationPolicyProvider(Options.Create(options));
    }

    private static PermissionRequirement RequirementOf(AuthorizationPolicy policy)
        => policy.Requirements.OfType<PermissionRequirement>().Single();

    [Test]
    public async Task AdministratorOrUserPolicyName_WhenGetPolicyAsync_ShouldCarryTheTagAndAllowAdministrator()
    {
        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        RequirementOf(policy).Tag.Should().Be(SomeTag);
        RequirementOf(policy).AllowAdministrator.Should().BeTrue();
    }

    [Test]
    public async Task UserPolicyName_WhenGetPolicyAsync_ShouldCarryTheTagAndNotAllowAdministrator()
    {
        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: false));

        RequirementOf(policy).Tag.Should().Be(SomeTag);
        RequirementOf(policy).AllowAdministrator.Should().BeFalse();
    }

    [Test]
    public async Task PermissionPolicyName_WhenGetPolicyAsync_ShouldDenyAnonymous()
    {
        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        policy.Requirements.Should().ContainItemsAssignableTo<DenyAnonymousAuthorizationRequirement>(
            "the fallback policy does not run on endpoints carrying an AuthorizeAttribute");
    }

    [Test]
    public async Task TagContainingAColon_WhenGetPolicyAsync_ShouldPreserveTheWholeTag()
    {
        var tagWithColon = "Module:CanEdit";

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(tagWithColon, allowAdministrator: true));

        RequirementOf(policy).Tag.Should().Be(tagWithColon);
    }

    [Test]
    public async Task UnrecognizedMode_WhenGetPolicyAsync_ShouldReturnNull()
    {
        var policy = await provider.GetPolicyAsync($"{PermissionPolicy.Prefix}Bogus:{SomeTag}");

        policy.Should().BeNull("an unrecognized mode must not degrade to a weaker gate");
    }

    [Test]
    public async Task PrefixWithNoTag_WhenGetPolicyAsync_ShouldReturnNull()
    {
        var policy = await provider.GetPolicyAsync($"{PermissionPolicy.Prefix}AdministratorOrUser:");

        policy.Should().BeNull();
    }

    [Test]
    public async Task PrefixOnly_WhenGetPolicyAsync_ShouldReturnNull()
    {
        var policy = await provider.GetPolicyAsync(PermissionPolicy.Prefix);

        policy.Should().BeNull();
    }

    [Test]
    public async Task ExistingPolicyName_WhenGetPolicyAsync_ShouldDelegateToTheDefaultProvider()
    {
        var policy = await provider.GetPolicyAsync("ExistingPolicy");

        policy.Should().NotBeNull();
        policy.Requirements.OfType<PermissionRequirement>().Should().BeEmpty();
    }

    [Test]
    public async Task UnknownPolicyName_WhenGetPolicyAsync_ShouldReturnNull()
    {
        var policy = await provider.GetPolicyAsync("NoSuchPolicy");

        policy.Should().BeNull();
    }

    [Test]
    public async Task FallbackPolicyWithSchemes_WhenGetPolicyAsync_ShouldCopyTheSchemeList()
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("SchemeOne", "SchemeTwo")
            .Build();

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        policy.AuthenticationSchemes.Should().BeEquivalentTo(["SchemeOne", "SchemeTwo"]);
    }

    [Test]
    public async Task NoFallbackPolicy_WhenGetPolicyAsync_ShouldStillProduceAPolicy()
    {
        options.FallbackPolicy = null;

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        policy.Should().NotBeNull();
        policy.AuthenticationSchemes.Should().BeEmpty();
    }

    [Test]
    public void BothAttributes_WhenConstructed_ShouldSetMatchingPolicyNames()
    {
        new AdministratorOrUserWithPermissionAttribute(SomeTag).Policy
            .Should().Be(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));
        new UserWithPermissionAttribute(SomeTag).Policy
            .Should().Be(PermissionPolicy.NameFor(SomeTag, allowAdministrator: false));
    }

    [Test]
    public void Provider_WhenAskedIfPoliciesCanBeCached_ShouldAllowIt()
    {
        provider.AllowsCachingPolicies.Should().BeTrue();
    }

    [Test]
    public async Task ConfiguredDefaultPolicy_WhenGetDefaultPolicyAsync_ShouldReturnIt()
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireClaim("DistinguishableDefaultPolicyClaim")
            .Build();
        provider = new PermissionAuthorizationPolicyProvider(Options.Create(options));

        var policy = await provider.GetDefaultPolicyAsync();

        policy.Should().BeSameAs(options.DefaultPolicy);
    }

    [Test]
    public async Task ConfiguredFallbackPolicy_WhenGetFallbackPolicyAsync_ShouldReturnIt()
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireClaim("DistinguishableFallbackPolicyClaim")
            .Build();
        provider = new PermissionAuthorizationPolicyProvider(Options.Create(options));

        var policy = await provider.GetFallbackPolicyAsync();

        policy.Should().BeSameAs(options.FallbackPolicy);
    }

    [Test]
    public async Task NullPolicyName_WhenGetPolicyAsync_ShouldThrowArgumentNullException()
    {
        var act = async () => await provider.GetPolicyAsync(null);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task FallbackPolicyWithRoleRequirement_WhenGetPolicyAsync_ShouldInheritIt()
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireRole("Administrator")
            .Build();

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        policy.Requirements.Should().ContainItemsAssignableTo<RolesAuthorizationRequirement>(
            "a permission attribute must only make an endpoint stricter, never looser");
    }

    [Test]
    public async Task FallbackPolicyWithCustomRequirement_WhenGetPolicyAsync_ShouldInheritIt()
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireClaim("tenant")
            .Build();

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        policy.Requirements.Should().ContainItemsAssignableTo<ClaimsAuthorizationRequirement>(
            "a permission attribute must only make an endpoint stricter, never looser");
    }

    [Test]
    public async Task PolicyExplicitlyRegisteredWithThePermissionPrefix_WhenGetPolicyAsync_ShouldReturnTheRegisteredOne()
    {
        var policyName = PermissionPolicy.NameFor("X", allowAdministrator: true);
        options.AddPolicy(policyName, policy => policy.RequireAssertion(_ => true));
        provider = new PermissionAuthorizationPolicyProvider(Options.Create(options));

        var policy = await provider.GetPolicyAsync(policyName);

        policy.Requirements.OfType<PermissionRequirement>().Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void BlankTag_WhenNameFor_ShouldThrowArgumentException(string tag)
    {
        var act = () => PermissionPolicy.NameFor(tag, allowAdministrator: true);

        act.Should().Throw<ArgumentException>();
    }
}
