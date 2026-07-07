using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Authentication.Certificate;
using Cause.SecurityManagement.Core.Repositories;
using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication.Certificate;

[TestFixture]
public class CertificateAuthenticationHandlerTests
{
    private const string CertificateSubject = "CN=some-system,O=CAUCA";

    private IHost apiHost;
    private TestServer apiServer;
    private ICertificateValidator certificateValidator;
    private IExternalSystemRepository repository;

    [TearDown]
    public async Task TearDownTest()
    {
        if (apiHost != null)
            await apiHost.StopAsync();
        apiHost?.Dispose();
    }

    [Test]
    public async Task ExternalSystemAuthenticatedByCertificate_WhenAuthenticated_ShouldIncludeCertificateAuthenticationTypeClaim()
    {
        var externalSystem = new ExternalSystem
        {
            Name = "some-system",
            AuthenticationType = ExternalSystemAuthenticationType.Certificate,
        };
        await StartHostAsync(externalSystem);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be(nameof(ExternalSystemAuthenticationType.Certificate));
    }

    [Test]
    public async Task ExternalSystemAuthenticatedByToken_WhenAuthenticatedThroughCertificateHandler_ShouldIncludeTokenAuthenticationTypeClaim()
    {
        var externalSystem = new ExternalSystem
        {
            Name = "some-system",
            AuthenticationType = ExternalSystemAuthenticationType.Token,
        };
        await StartHostAsync(externalSystem);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be(nameof(ExternalSystemAuthenticationType.Token));
    }

    [Test]
    public async Task UnknownCertificateSubject_WhenAuthenticated_ShouldFail()
    {
        await StartHostAsync(externalSystem: null);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task StartHostAsync(ExternalSystem externalSystem)
    {
        certificateValidator = Substitute.For<ICertificateValidator>();
        certificateValidator.GetUserDn().Returns(CertificateSubject);
        repository = Substitute.For<IExternalSystemRepository>();
        repository.GetByCertificateSubject(Arg.Is(CertificateSubject)).Returns(externalSystem);

        var builder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(certificateValidator);
                    services.AddSingleton(repository);
                    services.AddRouting();
                    services.AddExternalCertificateAuthentication();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/secure", async context =>
                        {
                            var result = await context.AuthenticateAsync(CustomAuthSchemes.CertificateAuthentication);
                            if (!result.Succeeded)
                            {
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                return;
                            }

                            context.Response.StatusCode = StatusCodes.Status200OK;
                            await context.Response.WriteAsync(result.Principal.FindFirst(ExternalSystemClaims.AuthenticationType)?.Value ?? string.Empty);
                        });
                    });
                });
            });

        apiHost = await builder.StartAsync();
        apiServer = apiHost.GetTestServer();
    }
}
