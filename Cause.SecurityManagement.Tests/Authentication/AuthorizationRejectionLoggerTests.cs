using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Authentication.Certificate;
using Cause.SecurityManagement.Core.Authentication.Exceptions;
using Cause.SecurityManagement.Core.Repositories;
using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class AuthorizationRejectionLoggerTests
{
    private const string CertificateSubject = "CN=some-system,O=CAUCA";
    private const string JwtSchemeName = "TestJwtScheme";
    private const string JwtIssuer = "test-issuer";
    private const string JwtAudience = "test-audience";
    private const string ValidJwtSecretKey = "valid-secret-key-with-enough-length-1234567890";
    private const string WrongJwtSecretKey = "wrong-secret-key-with-enough-length-0987654321";

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
    public async Task NoCredentialsAtAll_WhenCallingProtectedEndpoint_ShouldLogExactlyOneEntryNamingNoCredentialsPresented()
    {
        await StartHostAsync(certificateNotPresented: true);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure-cert");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var entries = RejectionLoggerEntries();
        entries.Should().HaveCount(1);
        var message = entries.Single().Message;
        message.Should().Contain("no credentials were presented");
        CountOccurrences(message, CustomAuthSchemes.CertificateAuthentication).Should().Be(1,
            because: "the scheme name must appear once (in the reason), not once for a scheme-list placeholder and again in the reason");
        entries.Single().Scope.Should().ContainSingle(kvp =>
            kvp.Key == "RejectionScheme" && Equals(kvp.Value, CustomAuthSchemes.CertificateAuthentication));
    }

    [Test]
    public async Task InvalidCertificate_WhenCallingProtectedEndpoint_ShouldLogExactlyOneEntryWithTheRealReason()
    {
        await StartHostAsync(externalSystem: null);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure-cert");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var entries = RejectionLoggerEntries();
        entries.Should().HaveCount(1);
        entries.Single().Message.Should().Contain($"No active external system is registered for certificate subject DN '{CertificateSubject}'");
    }

    [Test]
    public async Task InvalidCertificate_WhenCallingProtectedEndpoint_ShouldNotResurfaceTheFailureOutsideTheRejectionLoggerCategory()
    {
        await StartHostAsync(externalSystem: null);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure-cert");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var entriesOutsideRejectionLoggerCategory = capturingLoggerProvider.Entries
            .Where(entry => entry.Category != typeof(AuthorizationRejectionLogger).FullName)
            .ToList();
        entriesOutsideRejectionLoggerCategory
            .Any(entry => entry.Exception is ExternalSystemNotFound or CertificateNotValidException)
            .Should().BeFalse();
    }

    [Test]
    public async Task InvalidBearerToken_WhenCallingProtectedEndpoint_ShouldLogExactlyOneEntryNamingTheTokenFailure()
    {
        await StartHostAsync(externalSystem: null);
        using var client = apiServer.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateSignedJwt(WrongJwtSecretKey));

        var response = await client.GetAsync("/secure-jwt");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var entries = RejectionLoggerEntries();
        entries.Should().HaveCount(1);
        entries.Single().Message.Should().Contain("Signature validation failed");
    }

    [Test]
    public async Task AllowAnonymousEndpoint_WhenNoCredentialsAtAll_ShouldLogNothing()
    {
        await StartHostAsync(certificateNotPresented: true);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/anonymous");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RejectionLoggerEntries().Should().BeEmpty();
    }

    [Test]
    public async Task SuccessfulAuthentication_WhenCallingProtectedEndpoint_ShouldLogNothing()
    {
        var externalSystem = new ExternalSystem
        {
            Name = "some-system",
            AuthenticationType = ExternalSystemAuthenticationType.Certificate,
        };
        await StartHostAsync(externalSystem);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure-cert");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RejectionLoggerEntries().Should().BeEmpty();
    }

    [Test]
    public async Task LoggingFaults_WhenCallingProtectedEndpoint_ShouldStillDelegateAndReturn401()
    {
        await StartHostAsync(certificateNotPresented: true, useThrowingLogger: true);
        using var client = apiServer.CreateClient();

        var act = async () => await client.GetAsync("/secure-cert");

        var response = await act.Should().NotThrowAsync();
        response.Subject.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task VeryLongFailureMessage_WhenCallingProtectedEndpoint_ShouldTruncateTheReason()
    {
        var longCertificateSubjectDn = "CN=" + new string('a', 700);
        await StartHostAsync(externalSystem: null, certificateSubjectDn: longCertificateSubjectDn);
        using var client = apiServer.CreateClient();

        var response = await client.GetAsync("/secure-cert");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var entries = RejectionLoggerEntries();
        entries.Should().HaveCount(1);
        const string expectedPrefix = "Request to /secure-cert was rejected with 401. ";
        var message = entries.Single().Message;
        message.Should().StartWith(expectedPrefix);
        var reasonPart = message[expectedPrefix.Length..];
        reasonPart.Length.Should().Be(512);
        reasonPart.Should().EndWith("...[truncated]");
    }

    private static int CountOccurrences(string haystack, string needle) =>
        haystack.Split(needle, StringSplitOptions.None).Length - 1;

    private List<CapturedLogEntry> RejectionLoggerEntries() =>
        capturingLoggerProvider.Entries
            .Where(entry => entry.Category == typeof(AuthorizationRejectionLogger).FullName)
            .ToList();

    private static string CreateSignedJwt(string secretKey)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(ClaimTypes.Name, "some-caller") };
        var token = new JwtSecurityToken(
            JwtIssuer,
            JwtAudience,
            claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return handler.WriteToken(token);
    }

    private async Task StartHostAsync(
        ExternalSystem externalSystem = null,
        bool certificateNotPresented = false,
        bool useThrowingLogger = false,
        string certificateSubjectDn = CertificateSubject)
    {
        certificateValidator = Substitute.For<ICertificateValidator>();
        if (certificateNotPresented)
        {
            certificateValidator
                .When(validator => validator.ValidateCertificate(Arg.Any<IHeaderDictionary>()))
                .Do(_ => throw new CertificateNotPresentException());
        }
        else
        {
            certificateValidator.GetUserDn().Returns(certificateSubjectDn);
        }

        repository = Substitute.For<IExternalSystemRepository>();
        repository.GetByCertificateSubject(Arg.Is(certificateSubjectDn)).Returns(externalSystem);
        capturingLoggerProvider = new CapturingLoggerProvider();

        var builder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureLogging(logging => logging
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddProvider(capturingLoggerProvider));
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(certificateValidator);
                    services.AddSingleton(repository);
                    services.AddRouting();
                    services.AddAuthorization();
                    services.AddAuthorizationRejectionLogging();
                    if (useThrowingLogger)
                        services.AddSingleton<ILogger<AuthorizationRejectionLogger>>(new AlwaysThrowingLogger());
                    services.AddExternalCertificateAuthentication();
                    services.AddAuthentication().AddJwtBearer(JwtSchemeName, options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidJwtSecretKey)),
                            ValidateIssuer = true,
                            ValidIssuer = JwtIssuer,
                            ValidateAudience = true,
                            ValidAudience = JwtAudience,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero
                        };
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/anonymous", RespondOkAsync).AllowAnonymous();
                        endpoints.MapGet("/secure-cert", RespondOkAsync).RequireAuthorization();
                        endpoints.MapGet("/secure-jwt", RespondOkAsync).RequireAuthorization(policy => policy
                            .AddAuthenticationSchemes(JwtSchemeName)
                            .RequireAuthenticatedUser());
                    });
                });
            });

        apiHost = await builder.StartAsync();
        apiServer = apiHost.GetTestServer();
        return;

        static async Task RespondOkAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync(string.Empty);
        }
    }

    private sealed class AlwaysThrowingLogger : ILogger<AuthorizationRejectionLogger>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            throw new InvalidOperationException("BeginScope always fails in this test.");

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter) =>
            throw new InvalidOperationException("Log always fails in this test.");
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
            private readonly AsyncLocal<object> currentScope = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                var previous = currentScope.Value;
                currentScope.Value = state;
                return new RestoreScope(currentScope, previous);
            }

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
                    state as IReadOnlyList<KeyValuePair<string, object>>,
                    currentScope.Value as IReadOnlyList<KeyValuePair<string, object>>));
            }

            private sealed class RestoreScope(AsyncLocal<object> scope, object previous) : IDisposable
            {
                public void Dispose() => scope.Value = previous;
            }
        }
    }

    private sealed record CapturedLogEntry(
        string Category,
        LogLevel LogLevel,
        string Message,
        Exception Exception,
        IReadOnlyList<KeyValuePair<string, object>> State,
        IReadOnlyList<KeyValuePair<string, object>> Scope);
}
