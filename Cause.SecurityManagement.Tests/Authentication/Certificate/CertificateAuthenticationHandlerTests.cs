using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Authentication.Certificate;
using Cause.SecurityManagement.Core.Authentication.Exceptions;
using Cause.SecurityManagement.Core.Repositories;
using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private CapturingLoggerProvider capturingLoggerProvider;

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

    [Test]
    public async Task DuplicateCertificateSubject_WhenAuthenticated_ShouldReturnInternalServerError()
    {
        await StartHostAsync(externalSystem: null, repositoryThrowsDuplicateCertificateSubjectException: true, useExceptionHandlerMiddleware: true);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task DuplicateCertificateSubject_WhenAuthenticated_ShouldLogErrorNamingTheCertificateSubjectDn()
    {
        await StartHostAsync(externalSystem: null, repositoryThrowsDuplicateCertificateSubjectException: true, useExceptionHandlerMiddleware: true);
        using var client = apiServer.CreateClient();

        await client.GetAsync("/secure");

        var errorEntries = capturingLoggerProvider.Entries
            .Where(entry => entry.Category == typeof(CertificateAuthenticationHandler).FullName)
            .Where(entry => entry.LogLevel == LogLevel.Error)
            .ToList();
        errorEntries.Should().HaveCount(1);
        var errorEntry = errorEntries.Single();
        errorEntry.State.Should().ContainSingle(state =>
            state.Key == "CertificateSubjectDn" && Equals(state.Value, CertificateSubject));
        errorEntry.Exception.Should().BeOfType<DuplicateCertificateSubjectException>();
    }

    private async Task StartHostAsync(
        ExternalSystem externalSystem,
        bool repositoryThrowsDuplicateCertificateSubjectException = false,
        bool useExceptionHandlerMiddleware = false)
    {
        certificateValidator = Substitute.For<ICertificateValidator>();
        certificateValidator.GetUserDn().Returns(CertificateSubject);
        repository = Substitute.For<IExternalSystemRepository>();
        capturingLoggerProvider = new CapturingLoggerProvider();
        if (repositoryThrowsDuplicateCertificateSubjectException)
        {
            repository.GetByCertificateSubject(Arg.Any<string>())
                .Returns(_ => throw new DuplicateCertificateSubjectException(CertificateSubject));
        }
        else
        {
            repository.GetByCertificateSubject(Arg.Is(CertificateSubject)).Returns(externalSystem);
        }

        var builder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureLogging(logging => logging.AddProvider(capturingLoggerProvider));
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(certificateValidator);
                    services.AddSingleton(repository);
                    services.AddRouting();
                    services.AddExternalCertificateAuthentication();
                });
                webBuilder.Configure(app =>
                {
                    if (useExceptionHandlerMiddleware)
                    {
                        app.UseExceptionHandler(errorApp =>
                        {
                            errorApp.Run(async context =>
                            {
                                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                                await context.Response.WriteAsync(string.Empty);
                            });
                        });
                    }

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

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<CapturedLogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(string categoryName, List<CapturedLogEntry> entries) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                entries.Add(new CapturedLogEntry(
                    categoryName,
                    logLevel,
                    formatter(state, exception),
                    exception,
                    state as IReadOnlyList<KeyValuePair<string, object>>));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed record CapturedLogEntry(
        string Category,
        LogLevel LogLevel,
        string Message,
        Exception Exception,
        IReadOnlyList<KeyValuePair<string, object>> State);
}
