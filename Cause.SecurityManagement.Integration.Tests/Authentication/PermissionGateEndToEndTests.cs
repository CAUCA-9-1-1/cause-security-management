using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Authentication;

/// <summary>
/// Drives the permission gate through a real minimal-API pipeline backed by a real PostgreSQL
/// database, proving what the repository tests and the stubbed HTTP pipeline tests cannot: that
/// DI resolves the real IUserPermissionService inside the handler's request scope, and that the
/// Sid claim on the principal is actually used to filter the UserPermission/GroupPermission rows.
/// A mismatched Sid returning 200 here would mean every authenticated RegularUser passes every
/// gate — a critical finding, not a test bug.
/// </summary>
[TestFixture]
public class PermissionGateEndToEndTests
{
    private const string GrantedTag = "CanEditBuilding";
    private const string TestScheme = "TestScheme";

    private TestSecurityContext context = null!;
    private IHost host = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        context = DatabaseFixture.CreateContext();
        host = await CreateHostAsync();
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        if (host is not null)
        {
            await host.StopAsync();
            host.Dispose();
        }

        await context.DisposeAsync();
    }

    private async Task<IHost> CreateHostAsync()
    {
        var builder = new HostBuilder().ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseTestServer();
            webBuilder.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton<ISecurityContext<TestUser>>(context);
                services.AddSingleton(context);
                services.AddAuthentication(TestScheme)
                    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(TestScheme, _ => { });
                services.AddAuthorizationForRegularUser();
                services.AddPermissionBasedAuthorization();
                services.InjectSecurityServices<TestUser>();
            });
            webBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/granted", Ok)
                        .RequireAuthorization(PermissionPolicy.NameFor(GrantedTag, allowAdministrator: true));
                });
            });
        });
        return await builder.StartAsync();
    }

    private static Task Ok(HttpContext httpContext) => httpContext.Response.WriteAsync("ok");

    private async Task<HttpStatusCode> GetGrantedAsync(Guid sid)
    {
        using var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", SecurityRoles.User);
        client.DefaultRequestHeaders.Add("X-Test-Sid", sid.ToString());

        var response = await client.GetAsync("/granted");
        return response.StatusCode;
    }

    private sealed class HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim> { new(ClaimTypes.Role, role.ToString()) };
            if (Request.Headers.TryGetValue("X-Test-Sid", out var sid))
                claims.Add(new Claim(JwtRegisteredClaimNames.Sid, sid.ToString()));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, TestScheme));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, TestScheme)));
        }
    }

    [Test]
    public async Task DirectPermissionAllowed_WhenCallingGrantedEndpoint_ShouldReturnOk()
    {
        var user = SeedUser();
        var permission = SeedModulePermission(GrantedTag);
        SeedUserPermission(user, permission, isAllowed: true);

        var status = await GetGrantedAsync(user.Id);

        status.Should().Be(HttpStatusCode.OK, "the full chain — claim, repository, and handler — must connect");
    }

    [Test]
    public async Task DirectPermissionDenied_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
    {
        var user = SeedUser();
        var permission = SeedModulePermission(GrantedTag);
        SeedUserPermission(user, permission, isAllowed: false);

        var status = await GetGrantedAsync(user.Id);

        status.Should().Be(HttpStatusCode.Forbidden, "IsAllowed = false must be honored end to end, not merely parsed");
    }

    [Test]
    public async Task NoPermissionRow_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
    {
        var user = SeedUser();

        var status = await GetGrantedAsync(user.Id);

        status.Should().Be(HttpStatusCode.Forbidden, "absence of a permission row must deny");
    }

    [Test]
    public async Task PermissionGrantedThroughGroup_WhenCallingGrantedEndpoint_ShouldReturnOk()
    {
        var user = SeedUser();
        var permission = SeedModulePermission(GrantedTag);
        var group = SeedGroup();
        SeedGroupPermission(group, permission, isAllowed: true);
        SeedUserGroup(user, group);

        var status = await GetGrantedAsync(user.Id);

        status.Should().Be(HttpStatusCode.OK, "the group query path must also connect end to end");
    }

    [Test]
    public async Task GroupDeniesWhileUserRowAllowsSameTag_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
    {
        var user = SeedUser();
        var permission = SeedModulePermission(GrantedTag);
        SeedUserPermission(user, permission, isAllowed: true);
        var group = SeedGroup();
        SeedGroupPermission(group, permission, isAllowed: false);
        SeedUserGroup(user, group);

        var status = await GetGrantedAsync(user.Id);

        status.Should().Be(HttpStatusCode.Forbidden, "deny-wins must survive the whole chain, not just PermissionMergeTool");
    }

    [Test]
    public async Task SidMatchingNoUser_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
    {
        var user = SeedUser();
        var permission = SeedModulePermission(GrantedTag);
        SeedUserPermission(user, permission, isAllowed: true);

        var status = await GetGrantedAsync(Guid.NewGuid());

        status.Should().Be(HttpStatusCode.Forbidden, "the Sid claim must actually be used to look up rows");
    }

    [Test]
    public async Task SameUser_WhenCallingGrantedEndpointTwice_ShouldReturnOkBothTimes()
    {
        var user = SeedUser();
        var permission = SeedModulePermission(GrantedTag);
        SeedUserPermission(user, permission, isAllowed: true);

        var firstStatus = await GetGrantedAsync(user.Id);
        var secondStatus = await GetGrantedAsync(user.Id);

        firstStatus.Should().Be(HttpStatusCode.OK);
        secondStatus.Should().Be(HttpStatusCode.OK, "the per-request cache must not leak or corrupt state across requests");
    }

    [Test]
    public async Task SyncAndAsyncPermissionPaths_WhenBothSourcesContributeWithADenial_ShouldAgree()
    {
        var user = SeedUser();
        var permission = SeedModulePermission("SyncAsyncEquivalenceTag");
        SeedUserPermission(user, permission, isAllowed: true);
        var group = SeedGroup();
        SeedGroupPermission(group, permission, isAllowed: false);
        SeedUserGroup(user, group);

        using var scope = host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserPermissionService>();

        var synchronous = service.GetPermissionsForUser(user.Id);
        var asynchronous = await service.GetPermissionsForUserAsync(user.Id, CancellationToken.None);

        asynchronous.Should().BeEquivalentTo(synchronous,
            "the sync and async paths run different SQL and must not answer differently");
    }

    private TestUser SeedUser()
    {
        var user = new TestUser
        {
            UserName = $"user_{Guid.NewGuid():N}",
            Password = "x",
            Email = $"{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private Group SeedGroup()
    {
        var group = new Group { Id = Guid.NewGuid(), Name = $"group_{Guid.NewGuid():N}" };
        context.Groups.Add(group);
        context.SaveChanges();
        return group;
    }

    private ModulePermission SeedModulePermission(string tag)
    {
        var module = new Module { Id = Guid.NewGuid(), Name = $"module_{Guid.NewGuid():N}", Tag = $"mod_{Guid.NewGuid():N}" };
        context.Modules.Add(module);
        var permission = new ModulePermission
        {
            Id = Guid.NewGuid(),
            IdModule = module.Id,
            Tag = tag,
            Name = $"name_{Guid.NewGuid():N}",
        };
        context.ModulePermissions.Add(permission);
        context.SaveChanges();
        return permission;
    }

    private void SeedUserPermission(TestUser user, ModulePermission permission, bool isAllowed)
    {
        context.UserPermissions.Add(new UserPermission
        {
            Id = Guid.NewGuid(),
            IdUser = user.Id,
            IdModulePermission = permission.Id,
            IsAllowed = isAllowed,
        });
        context.SaveChanges();
    }

    private void SeedGroupPermission(Group group, ModulePermission permission, bool isAllowed)
    {
        context.GroupPermissions.Add(new GroupPermission
        {
            Id = Guid.NewGuid(),
            IdGroup = group.Id,
            IdModulePermission = permission.Id,
            IsAllowed = isAllowed,
        });
        context.SaveChanges();
    }

    private void SeedUserGroup(TestUser user, Group group)
    {
        context.UserGroups.Add(new UserGroup
        {
            Id = Guid.NewGuid(),
            IdUser = user.Id,
            IdGroup = group.Id,
        });
        context.SaveChanges();
    }
}
