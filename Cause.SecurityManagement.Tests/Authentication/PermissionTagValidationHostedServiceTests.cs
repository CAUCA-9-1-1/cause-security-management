using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class PermissionTagValidationHostedServiceTests
{
    private static PermissionTagValidationHostedService CreateService(
        IPermissionCatalogService catalogService,
        FakeLogger<PermissionTagValidationHostedService> logger,
        params string[] policies)
    {
        var services = new ServiceCollection();
        if (catalogService is not null)
            services.AddScoped(_ => catalogService);

        var provider = services.BuildServiceProvider();
        var dataSource = new StubEndpointDataSource(policies);

        return new PermissionTagValidationHostedService(
            [dataSource],
            provider.GetRequiredService<IServiceScopeFactory>(),
            logger);
    }

    [Test]
    public async Task AllTagsKnown_WhenStarting_ShouldNotWarn()
    {
        var catalogService = Substitute.For<IPermissionCatalogService>();
        catalogService.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<PermissionDto> { new() { Tag = "CanEditBuilding" } }));
        var logger = new FakeLogger<PermissionTagValidationHostedService>();
        var service = CreateService(catalogService, logger, PermissionPolicy.NameFor("CanEditBuilding", true));

        await service.StartAsync(CancellationToken.None);

        logger.Warnings.Should().BeEmpty();
    }

    [Test]
    public async Task UnknownTag_WhenStarting_ShouldWarnNamingTheTag()
    {
        var catalogService = Substitute.For<IPermissionCatalogService>();
        catalogService.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<PermissionDto>()));
        var logger = new FakeLogger<PermissionTagValidationHostedService>();
        var service = CreateService(catalogService, logger, PermissionPolicy.NameFor("MissingTag", true));

        await service.StartAsync(CancellationToken.None);

        logger.Warnings.Should().ContainSingle(warning => warning.Contains("MissingTag"));
    }

    [Test]
    public async Task TagDifferingOnlyByCase_WhenStarting_ShouldWarn()
    {
        var catalogService = Substitute.For<IPermissionCatalogService>();
        catalogService.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<PermissionDto> { new() { Tag = "CanEditBuilding" } }));
        var logger = new FakeLogger<PermissionTagValidationHostedService>();
        var service = CreateService(catalogService, logger, PermissionPolicy.NameFor("caneditbuilding", true));

        await service.StartAsync(CancellationToken.None);

        logger.Warnings.Should().ContainSingle(
            "the runtime permission gate compares tags ordinally, so a case-only mismatch must also warn");
    }

    [Test]
    public async Task NonPermissionPolicies_WhenStarting_ShouldBeIgnored()
    {
        var catalogService = Substitute.For<IPermissionCatalogService>();
        catalogService.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<PermissionDto>()));
        var logger = new FakeLogger<PermissionTagValidationHostedService>();
        var service = CreateService(catalogService, logger, "SomeOtherPolicy");

        await service.StartAsync(CancellationToken.None);

        logger.Warnings.Should().BeEmpty();
    }

    [Test]
    public async Task NoEndpoints_WhenStarting_ShouldNotCallTheCatalog()
    {
        var catalogService = Substitute.For<IPermissionCatalogService>();
        var logger = new FakeLogger<PermissionTagValidationHostedService>();
        var service = CreateService(catalogService, logger);

        await service.StartAsync(CancellationToken.None);

        await catalogService.DidNotReceive().GetPermissionsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CatalogThrows_WhenStarting_ShouldWarnAndNotThrow()
    {
        var catalogService = Substitute.For<IPermissionCatalogService>();
        catalogService.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns<Task<List<PermissionDto>>>(_ => throw new InvalidOperationException("boom"));
        var logger = new FakeLogger<PermissionTagValidationHostedService>();
        var service = CreateService(catalogService, logger, PermissionPolicy.NameFor("SomeTag", true));

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        logger.Warnings.Should().ContainSingle(warning => warning.Contains("skipped"));
    }

    [Test]
    public async Task CatalogNotRegistered_WhenStarting_ShouldWarnAndNotThrow()
    {
        var logger = new FakeLogger<PermissionTagValidationHostedService>();
        var service = CreateService(null, logger, PermissionPolicy.NameFor("SomeTag", true));

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        logger.Warnings.Should().ContainSingle(warning => warning.Contains("skipped"));
    }

    [Test]
    public async Task BothAttributeModes_WhenStarting_ShouldValidateBothTags()
    {
        var catalogService = Substitute.For<IPermissionCatalogService>();
        catalogService.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<PermissionDto> { new() { Tag = "KnownTag" } }));
        var logger = new FakeLogger<PermissionTagValidationHostedService>();
        var service = CreateService(
            catalogService,
            logger,
            PermissionPolicy.NameFor("KnownTag", true),
            PermissionPolicy.NameFor("MissingTag", false));

        await service.StartAsync(CancellationToken.None);

        logger.Warnings.Should().ContainSingle(warning => warning.Contains("MissingTag"));
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

    private sealed class StubEndpointDataSource(params string[] policies) : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints { get; } = BuildEndpoints(policies);

        public override IChangeToken GetChangeToken() => throw new NotSupportedException();

        private static IReadOnlyList<Endpoint> BuildEndpoints(string[] policies)
        {
            var endpoints = new List<Endpoint>();
            foreach (var policy in policies)
            {
                var metadata = new EndpointMetadataCollection(new AuthorizeAttribute { Policy = policy });
                endpoints.Add(new Endpoint(_ => Task.CompletedTask, metadata, "test"));
            }
            return endpoints;
        }
    }
}
