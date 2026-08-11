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
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

/// <summary>
/// Exercises the permission gate through a real ASP.NET Core minimal-API pipeline rather than a
/// hand-built AuthorizationHandlerContext, so it proves the policy provider replacing the default,
/// the dynamic policy inheriting the fallback, and the handler resolving the cache from
/// HttpContext.RequestServices all actually compose.
/// Two framework interactions were discovered empirically while writing this fixture and are
/// recorded here rather than in a comment buried in the setup code:
/// (1) UseAuthorization's VerifyServicesRegistered demands the marker service that only the
/// framework's own AddAuthorization() registers; AddAuthorizationCore alone — which is what every
/// AddAuthorizationFor* extension calls — is not enough. AddPermissionBasedAuthorization() now
/// calls the bare services.AddAuthorization() itself, so a consuming minimal-API app no longer
/// needs to work around the startup crash that used to throw
/// "Please add all the required services by calling 'IServiceCollection.AddAuthorization'".
/// (2) AddAuthorizationForRegularUserKeycloakAndApiCertificate's fallback policy names three
/// authentication schemes via AddAuthenticationSchemes, which the dynamic policy inherits through
/// Combine. If the host has not registered a handler under each of those exact scheme names,
/// PolicyEvaluator.AuthenticateAsync throws InvalidOperationException before authorization ever
/// runs — on every request touching that fallback, gated or not — rather than failing as a 401 or
/// 403. HostBRegularUserKeycloakAndApiCertificate registers the test scheme under all three names
/// to work around this; a real application must register real handlers under
/// CustomAuthSchemes.KeycloakAuthentication, RegularUserAuthentication, and
/// ConsoleCertificateAuthentication or every request crashes.
/// </summary>
[TestFixture]
public class PermissionGateEndpointTests
{
    private const string GrantedTag = "CanEditBuilding";
    private const string TestScheme = "TestScheme";

    private IUserPermissionService permissionService;
    private Guid someUserId;

    [SetUp]
    public void SetUp()
    {
        someUserId = Guid.NewGuid();
        permissionService = Substitute.For<IUserPermissionService>();
        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new List<UserMergedPermission>
            {
                new() { FeatureName = GrantedTag, Access = true },
            }));
    }

    private async Task<IHost> CreateHostAsync(
        Action<IServiceCollection> configureAuthorization,
        Action<AuthenticationBuilder> configureAuthentication = null)
    {
        var builder = new HostBuilder().ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseTestServer();
            webBuilder.ConfigureServices(services =>
            {
                services.AddRouting();
                var authenticationBuilder = services.AddAuthentication(TestScheme)
                    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(TestScheme, _ => { });
                configureAuthentication?.Invoke(authenticationBuilder);
                configureAuthorization(services);
                services.AddPermissionBasedAuthorization();
                services.AddSingleton(permissionService);
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
                    endpoints.MapGet("/other", Ok)
                        .RequireAuthorization(PermissionPolicy.NameFor("CanDoSomethingElse", allowAdministrator: true));
                    endpoints.MapGet("/strict", Ok)
                        .RequireAuthorization(PermissionPolicy.NameFor(GrantedTag, allowAdministrator: false));
                    endpoints.MapGet("/ungated", Ok);
                    endpoints.MapGet("/anonymous", Ok)
                        .RequireAuthorization(PermissionPolicy.NameFor(GrantedTag, allowAdministrator: true))
                        .AllowAnonymous();
                });
            });
        });
        return await builder.StartAsync();
    }

    private static Task Ok(HttpContext context) => context.Response.WriteAsync("ok");

    private static async Task<HttpStatusCode> GetAsync(IHost host, string path, string role = null, string sid = null)
    {
        using var client = host.GetTestServer().CreateClient();
        if (role is not null)
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
        if (sid is not null)
            client.DefaultRequestHeaders.Add("X-Test-Sid", sid);

        var response = await client.GetAsync(path);
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

    [TestFixture]
    public class HostARegularUserOnly : PermissionGateEndpointTests
    {
        private IHost host;

        [SetUp]
        public async Task SetUpHostAsync()
        {
            host = await CreateHostAsync(services => services.AddAuthorizationForRegularUser());
        }

        [TearDown]
        public async Task TearDownHostAsync()
        {
            if (host is not null)
                await host.StopAsync();
            host?.Dispose();
        }

        [Test]
        public async Task NoAuthHeader_WhenCallingGrantedEndpoint_ShouldReturnUnauthorized()
        {
            var status = await GetAsync(host, "/granted");

            status.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public async Task RegularUserWithSeededSid_WhenCallingGrantedEndpoint_ShouldReturnOk()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.User, someUserId.ToString());

            status.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public async Task RegularUserWithSeededSid_WhenCallingOtherEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/other", SecurityRoles.User, someUserId.ToString());

            status.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task Administrator_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.Administrator, someUserId.ToString());

            status.Should().Be(HttpStatusCode.Forbidden,
                "the inherited RequireRole(RegularUser) fallback excludes Administrators under this registration");
        }

        [Test]
        public async Task Administrator_WhenCallingStrictEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/strict", SecurityRoles.Administrator, someUserId.ToString());

            status.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task ExternalSystem_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.ExternalSystem, someUserId.ToString());

            status.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task RegularUser_WhenCallingUngatedEndpoint_ShouldReturnOk()
        {
            var status = await GetAsync(host, "/ungated", SecurityRoles.User, someUserId.ToString());

            status.Should().Be(HttpStatusCode.OK, "the fallback policy still applies and admits RegularUser");
        }

        [Test]
        public async Task ExternalSystem_WhenCallingUngatedEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/ungated", SecurityRoles.ExternalSystem, someUserId.ToString());

            status.Should().Be(HttpStatusCode.Forbidden, "this proves the fallback policy is intact on undecorated endpoints");
        }

        [Test]
        public async Task NoAuthHeader_WhenCallingAnonymousEndpoint_ShouldReturnOk()
        {
            var status = await GetAsync(host, "/anonymous");

            status.Should().Be(HttpStatusCode.OK, "[AllowAnonymous] bypasses the gate entirely");
        }

        [Test]
        public async Task RegularUserWithUnparseableSid_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.User, "not-a-guid");

            status.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task RegularUserWithNoSid_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.User);

            status.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task GatedEndpoint_WhenCalledOnce_ShouldCallPermissionServiceExactlyOnce()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.User, someUserId.ToString());

            status.Should().Be(HttpStatusCode.OK);
            await permissionService.Received(1).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task GatedEndpoint_WhenCalledTwiceForSameUser_ShouldCallPermissionServiceTwice()
        {
            await GetAsync(host, "/granted", SecurityRoles.User, someUserId.ToString());
            await GetAsync(host, "/granted", SecurityRoles.User, someUserId.ToString());

            await permissionService.Received(2).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
        }
    }

    [TestFixture]
    public class HostBRegularUserKeycloakAndApiCertificate : PermissionGateEndpointTests
    {
        private IHost host;

        [SetUp]
        public async Task SetUpHostAsync()
        {
            host = await CreateHostAsync(
                services => services.AddAuthorizationForRegularUserKeycloakAndApiCertificate(),
                authenticationBuilder => authenticationBuilder
                    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(CustomAuthSchemes.KeycloakAuthentication, _ => { })
                    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(CustomAuthSchemes.RegularUserAuthentication, _ => { })
                    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(CustomAuthSchemes.ConsoleCertificateAuthentication, _ => { }));
        }

        [TearDown]
        public async Task TearDownHostAsync()
        {
            if (host is not null)
                await host.StopAsync();
            host?.Dispose();
        }

        [Test]
        public async Task Administrator_WhenCallingGrantedEndpoint_ShouldReturnOk()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.Administrator, someUserId.ToString());

            status.Should().Be(HttpStatusCode.OK,
                "this fallback admits Administrator, and the handler passes them without a lookup");
        }

        [Test]
        public async Task Administrator_WhenCallingStrictEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/strict", SecurityRoles.Administrator, someUserId.ToString());

            status.Should().Be(HttpStatusCode.Forbidden, "UserWithPermission denies Administrators");
        }

        [Test]
        public async Task RegularUserWithSeededSid_WhenCallingGrantedEndpoint_ShouldReturnOk()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.User, someUserId.ToString());

            status.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public async Task Console_WhenCallingGrantedEndpoint_ShouldReturnForbidden()
        {
            var status = await GetAsync(host, "/granted", SecurityRoles.ApiCertificate, someUserId.ToString());

            status.Should().Be(HttpStatusCode.Forbidden, "the fallback admits Console, but the handler denies it");
        }

        [Test]
        public async Task Console_WhenCallingUngatedEndpoint_ShouldReturnOk()
        {
            var status = await GetAsync(host, "/ungated", SecurityRoles.ApiCertificate, someUserId.ToString());

            status.Should().Be(HttpStatusCode.OK,
                "this proves the handler, not the fallback, is what denies Console on gated endpoints");
        }
    }
}
