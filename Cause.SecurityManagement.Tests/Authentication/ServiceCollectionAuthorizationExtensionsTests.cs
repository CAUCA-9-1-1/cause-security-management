using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class ServiceCollectionAuthorizationExtensionsTests
{
    [Test]
    public void AddAuthorizationForExternalSystem_WhenCalledWithDefaults_ShouldRegisterAuthorizationRejectionLoggerAsTheResultHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAuthorizationForExternalSystem();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        resolved.Should().BeOfType<AuthorizationRejectionLogger>();
    }

    [Test]
    public void AddAuthorizationForExternalSystem_WhenOptedOut_ShouldNotRegisterAuthorizationRejectionLogger()
    {
        var services = new ServiceCollection();

        services.AddAuthorizationForExternalSystem(logAuthorizationRejections: false);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IAuthorizationMiddlewareResultHandler>();

        // Observed: AddAuthorizationForExternalSystem calls AddAuthorizationCore, not the fuller
        // AddAuthorization - and only AddAuthorization registers the framework's own default
        // IAuthorizationMiddlewareResultHandler. So opting out here, without AddAuthorization()
        // also being called, leaves the service entirely unregistered rather than falling back
        // to the framework default.
        resolved.Should().BeNull();
    }

    [Test]
    public void AddAuthorizationForRegularUser_WhenCalledWithDefaults_ShouldRegisterAuthorizationRejectionLoggerAsTheResultHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAuthorizationForRegularUser();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        resolved.Should().BeOfType<AuthorizationRejectionLogger>();
    }

    [Test]
    public void AddAuthorizationForRegularUser_WhenOptedOut_ShouldNotRegisterAuthorizationRejectionLogger()
    {
        var services = new ServiceCollection();

        services.AddAuthorizationForRegularUser(logAuthorizationRejections: false);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IAuthorizationMiddlewareResultHandler>();
        resolved.Should().BeNull();
    }

    [Test]
    public void AddAuthorizationForExternalSystemAndAddAuthorizationRejectionLogging_WhenBothCalled_ShouldLeaveExactlyOneDescriptor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAuthorizationForExternalSystem();
        services.AddAuthorizationRejectionLogging();

        services.Count(descriptor => descriptor.ServiceType == typeof(IAuthorizationMiddlewareResultHandler))
            .Should().Be(1);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        resolved.Should().BeOfType<AuthorizationRejectionLogger>();
    }

    [Test]
    public void AddAuthorizationForExternalSystem_WhenACustomResultHandlerWasRegisteredFirst_ShouldStillWinAndWarnAboutTheDisplacement()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomResultHandler>();

        services.AddAuthorizationForExternalSystem();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        resolved.Should().BeOfType<AuthorizationRejectionLogger>();

        var warning = provider.GetRequiredService<DisplacedAuthorizationResultHandlerWarning>();
        warning.DisplacedTypeName.Should().Be(typeof(CustomResultHandler).FullName);

        var logger = new FakeLogger<AuthorizationResultHandlerDisplacementWarningService>();
        var hostedService = new AuthorizationResultHandlerDisplacementWarningService(warning, logger);

        hostedService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        logger.Warnings.Should().ContainSingle(message => message.Contains(typeof(CustomResultHandler).FullName));
    }

    [Test]
    public void AddAuthorizationForExternalSystem_WhenNoResultHandlerWasRegisteredFirst_ShouldNotRegisterADisplacementWarning()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAuthorizationForExternalSystem();

        var provider = services.BuildServiceProvider();
        provider.GetService<DisplacedAuthorizationResultHandlerWarning>().Should().BeNull();
    }

    [Test]
    public void AddAuthorizationRejectionLogging_WhenOnlyTheFrameworkDefaultWasRegisteredFirst_ShouldNotRegisterADisplacementWarning()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();

        services.AddAuthorizationRejectionLogging();

        var provider = services.BuildServiceProvider();
        provider.GetService<DisplacedAuthorizationResultHandlerWarning>().Should().BeNull();
        provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>().Should().BeOfType<AuthorizationRejectionLogger>();
    }

    private sealed class CustomResultHandler : IAuthorizationMiddlewareResultHandler
    {
        public Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult) => Task.CompletedTask;
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }
}
