using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class PermissionAuthorizationHandlerTests
{
    private const string SomeTag = "CanEditBuilding";

    private IUserPermissionService permissionService;
    private PermissionAuthorizationHandler handler;
    private ServiceProvider serviceProvider;
    private IHttpContextAccessor httpContextAccessor;
    private Guid someUserId;

    [SetUp]
    public void SetUp()
    {
        someUserId = Guid.NewGuid();
        permissionService = Substitute.For<IUserPermissionService>();
        GrantPermissions();

        var services = new ServiceCollection();
        services.AddSingleton(permissionService);
        services.AddScoped<ScopedPermissionCache>();
        serviceProvider = services.BuildServiceProvider();

        httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext
        {
            RequestServices = serviceProvider.CreateScope().ServiceProvider,
        });

        handler = new PermissionAuthorizationHandler(
            httpContextAccessor,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    [TearDown]
    public void TearDown() => serviceProvider?.Dispose();

    private void GrantPermissions(params string[] allowedTags)
    {
        var permissions = new List<UserMergedPermission>();
        foreach (var tag in allowedTags)
            permissions.Add(new UserMergedPermission { FeatureName = tag, Access = true });

        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(permissions));
    }

    private static ClaimsPrincipal PrincipalWith(string role, string sid)
    {
        var claims = new List<Claim>();
        if (role is not null)
            claims.Add(new Claim(ClaimTypes.Role, role));
        if (sid is not null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Sid, sid));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private async Task<bool> EvaluateAsync(ClaimsPrincipal principal, bool allowAdministrator)
    {
        var requirement = new PermissionRequirement(SomeTag, allowAdministrator);
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    private static ClaimsPrincipal KeycloakPrincipal()
    {
        var keycloakIdentity = new ClaimsIdentity(
            [new Claim("iss", "https://keycloak-test")], "Keycloak", "preferred_username", "role");
        var graftedIdentity = new ClaimsIdentity();
        graftedIdentity.AddClaim(new Claim(ClaimTypes.Role, SecurityRoles.Administrator));

        var principal = new ClaimsPrincipal(keycloakIdentity);
        principal.AddIdentity(graftedIdentity);
        return principal;
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RegularUserHoldingThePermission_WhenHandling_ShouldSucceed(bool allowAdministrator)
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, someUserId.ToString()), allowAdministrator);

        result.Should().BeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RegularUserWithoutThePermission_WhenHandling_ShouldNotSucceed(bool allowAdministrator)
    {
        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, someUserId.ToString()), allowAdministrator);

        result.Should().BeFalse();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RegularUserWithTheTagDenied_WhenHandling_ShouldNotSucceed(bool allowAdministrator)
    {
        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new List<UserMergedPermission> { new() { FeatureName = SomeTag, Access = false } }));

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, someUserId.ToString()), allowAdministrator);

        result.Should().BeFalse();
    }

    [Test]
    public async Task Administrator_WhenHandlingWithAdministratorAllowed_ShouldSucceedWithoutLoadingPermissions()
    {
        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.Administrator, someUserId.ToString()), allowAdministrator: true);

        result.Should().BeTrue();
        await permissionService.DidNotReceive().GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Administrator_WhenHandlingWithAdministratorNotAllowed_ShouldNotSucceedWithoutLoadingPermissions()
    {
        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.Administrator, someUserId.ToString()), allowAdministrator: false);

        result.Should().BeFalse("UserWithPermission excludes Administrators, which is the whole reason two attributes exist");
        await permissionService.DidNotReceive().GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [TestCase(SecurityRoles.ExternalSystem)]
    [TestCase(SecurityRoles.ApiCertificate)]
    [TestCase(SecurityRoles.UserCreation)]
    [TestCase(SecurityRoles.UserRecovery)]
    [TestCase(SecurityRoles.UserPasswordSetup)]
    [TestCase(SecurityRoles.UserLoginWithMultiFactor)]
    public async Task NonUserRole_WhenHandling_ShouldNotSucceedWithoutLoadingPermissions(string role)
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(role, someUserId.ToString()), allowAdministrator: true);

        result.Should().BeFalse();
        await permissionService.DidNotReceive().GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PrincipalWithoutRoleClaim_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(role: null, sid: someUserId.ToString()), allowAdministrator: true);

        result.Should().BeFalse();
    }

    [Test]
    public async Task RegularUserWithoutSidClaim_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, sid: null), allowAdministrator: true);

        result.Should().BeFalse();
    }

    [Test]
    public async Task RegularUserWithUnparseableSidClaim_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, "not-a-guid"), allowAdministrator: true);

        result.Should().BeFalse();
    }

    [Test]
    public async Task PrincipalWithNoIdentity_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(new ClaimsPrincipal(), allowAdministrator: true);

        result.Should().BeFalse();
    }

    [Test]
    public async Task UnauthenticatedIdentityWithUserRoleAndSid_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, SecurityRoles.User),
            new Claim(JwtRegisteredClaimNames.Sid, someUserId.ToString()),
        ]));

        var result = await EvaluateAsync(principal, allowAdministrator: true);

        result.Should().BeFalse(
            "an identity with no authentication type is not authenticated, even though IsInRole still matches");
    }

    [Test]
    public async Task NullHttpContext_WhenHandling_ShouldStillEvaluateThroughAChildScope()
    {
        GrantPermissions(SomeTag);
        httpContextAccessor.HttpContext.Returns((HttpContext)null);

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, someUserId.ToString()), allowAdministrator: true);

        result.Should().BeTrue();
    }

    [Test]
    public async Task TwoRequirementsWithOnlyOneTagHeld_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);
        var held = new PermissionRequirement(SomeTag, allowAdministrator: true);
        var notHeld = new PermissionRequirement("SomeOtherTag", allowAdministrator: true);
        var context = new AuthorizationHandlerContext(
            [held, notHeld],
            PrincipalWith(SecurityRoles.User, someUserId.ToString()),
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse(
            "stacked attributes AND, so succeeding one requirement must not satisfy the other");
        context.PendingRequirements.Should().Contain(notHeld);
        await permissionService.Received(1).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TwoRequirementsWithBothTagsHeld_WhenHandling_ShouldSucceed()
    {
        GrantPermissions(SomeTag, "SomeOtherTag");
        var first = new PermissionRequirement(SomeTag, allowAdministrator: true);
        var second = new PermissionRequirement("SomeOtherTag", allowAdministrator: true);
        var context = new AuthorizationHandlerContext(
            [first, second],
            PrincipalWith(SecurityRoles.User, someUserId.ToString()),
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        context.PendingRequirements.Should().BeEmpty();
    }

    [Test]
    public async Task PermissionLookupThrows_WhenHandling_ShouldPropagateRatherThanSucceed()
    {
        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<List<UserMergedPermission>>(_ => throw new InvalidOperationException("transient database fault"));
        var requirement = new PermissionRequirement(SomeTag, allowAdministrator: true);
        var context = new AuthorizationHandlerContext(
            [requirement], PrincipalWith(SecurityRoles.User, someUserId.ToString()), resource: null);

        var act = async () => await handler.HandleAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        context.HasSucceeded.Should().BeFalse();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task KeycloakPrincipal_WhenHandling_ShouldMatchAllowAdministrator(bool allowAdministrator)
    {
        var result = await EvaluateAsync(KeycloakPrincipal(), allowAdministrator);

        result.Should().Be(allowAdministrator,
            "this pins the two-identity shape that MultiJwtClaimsTransformer produces: the role claim lives on a " +
            "separate grafted identity from the primary Keycloak identity, and IsInRole must still see it");
    }
}
