using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using System;

namespace Cause.SecurityManagement.Integration.Tests.Infrastructure;

/// <summary>
/// Base class for all integration tests. Creates a fresh ServiceProvider and DbContext per test.
/// Uses TestContainers PostgreSQL (started once for the whole assembly by DatabaseFixture).
/// </summary>
public abstract class IntegrationTestBase
{
    protected TestSecurityContext Context { get; private set; } = null!;
    protected TestCurrentUserService CurrentUserService { get; private set; } = null!;
    protected TestValidationCodeSender ValidationCodeSender { get; private set; } = null!;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    private IServiceScope _scope = null!;

    protected static readonly SecurityConfiguration TestConfiguration = new()
    {
        PackageName = "IntegrationTests",
        Issuer = "test-issuer",
        SecretKey = "integration-test-secret-key-must-be-32chars!",
        RefreshTokenCanExpire = true,
    };

    [SetUp]
    public virtual void SetUp()
    {
        // Reset static state before each test
        SecurityManagementOptions.MultiFactorAuthenticationIsActivated = false;
        SecurityManagementOptions.ValidateCurrentPasswordOnPasswordChange = false;

        Context = DatabaseFixture.CreateContext();
        CurrentUserService = new TestCurrentUserService();
        ValidationCodeSender = new TestValidationCodeSender();

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        _scope = ServiceProvider.CreateScope();

        // UseMultiFactorAuthentication() sets the static flag to true when registering.
        // Reset it here so each test starts with MFA disabled; tests that need it set it explicitly.
        SecurityManagementOptions.MultiFactorAuthenticationIsActivated = false;
    }

    [TearDown]
    public virtual async Task TearDownAsync()
    {
        // Reset static state after each test
        SecurityManagementOptions.MultiFactorAuthenticationIsActivated = false;

        _scope?.Dispose();
        await Context.DisposeAsync();

        if (ServiceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Context registered as both its concrete type and the interface
        services.AddSingleton<ISecurityContext<TestUser>>(Context);
        services.AddSingleton(Context);

        // No real HTTP context in tests — use the stub accessor
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Security configuration
        services.Configure<SecurityConfiguration>(_ =>
        {
            _.PackageName = TestConfiguration.PackageName;
            _.Issuer = TestConfiguration.Issuer;
            _.SecretKey = TestConfiguration.SecretKey;
            _.RefreshTokenCanExpire = TestConfiguration.RefreshTokenCanExpire;
        });

        // Wire up all security services (uses InjectSecurityServices internally)
        services.InjectSecurityServices<TestUser>(opts =>
            opts.UseMultiFactorAuthentication<TestValidationCodeSender>());

        // Override the sender with our capturing test double (singleton so tests can read LastSentCode)
        services.AddSingleton<IAuthenticationValidationCodeSender<TestUser>>(ValidationCodeSender);

        // Override ICurrentUserService with our pre-configurable test double
        services.AddSingleton<ICurrentUserService>(CurrentUserService);
    }

    protected T Resolve<T>() where T : notnull
        => _scope.ServiceProvider.GetRequiredService<T>();

    protected string EncodePassword(string password)
        => new PasswordGenerator().EncodePassword(password, TestConfiguration.PackageName);

    protected DbContextOptions<TestSecurityContext> CreateDbOptions()
        => new DbContextOptionsBuilder<TestSecurityContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .Options;
}
