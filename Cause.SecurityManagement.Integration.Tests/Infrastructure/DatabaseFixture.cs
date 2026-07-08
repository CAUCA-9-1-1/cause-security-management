using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;

namespace Cause.SecurityManagement.Integration.Tests;

[SetUpFixture]
public class DatabaseFixture
{
    private static PostgreSqlContainer container = null!;

    public static string ConnectionString => container.GetConnectionString();

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        // Npgsql 6+ maps DateTime to timestamptz by default and requires DateTimeKind.Utc.
        // The library's models use unspecified DateTime; enable legacy behaviour for tests.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        container = new PostgreSqlBuilder("postgres:16-alpine")
            .Build();

        await container.StartAsync();

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() => await container.DisposeAsync();

    public static TestSecurityContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestSecurityContext>()
            .UseNpgsql(GetConnectionStringWithLocalTimezone())
            .Options;
        return new TestSecurityContext(options);
    }

    // Npgsql 10 + EF Core 10 translates DateTime.Now in LINQ to now() (PostgreSQL UTC function).
    // The library stores datetimes as local time (timestamp without time zone, legacy behaviour).
    // Setting the session timezone to the local machine timezone ensures the comparison works.
    private static string GetConnectionStringWithLocalTimezone()
    {
        string timezone = TimeZoneInfo.Local.Id;
        if (OperatingSystem.IsWindows())
            TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, out timezone!);
        return $"{ConnectionString};Timezone={timezone}";
    }
}
