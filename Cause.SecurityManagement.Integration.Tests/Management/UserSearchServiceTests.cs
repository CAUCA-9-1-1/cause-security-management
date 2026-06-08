using AwesomeAssertions;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Management;

[TestFixture]
public class UserSearchServiceTests : IntegrationTestBase
{
    private IUserSearchService Service => Resolve<IUserSearchService>();

    [Test]
    public async Task SearchUsers_ShouldMatchFirstOrLastNameCaseInsensitiveAndExcludeIds()
    {
        var token = $"Z{Guid.NewGuid():N}";
        var first = SeedUser($"{token}first", "Smith");
        var second = SeedUser("John", $"{token}last");
        SeedUser("Unrelated", "Person");

        var result = await Service.SearchUsersAsync(new UserSearchRequestDto
        {
            Query = token.ToUpper(),
            Skip = 0,
            Top = 50,
            ExcludedUserIds = [second.Id],
        });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Id.Should().Be(first.Id);
    }

    [Test]
    public async Task SearchUsers_ShouldPageAndReportTotalBeforePaging()
    {
        var token = $"Z{Guid.NewGuid():N}";
        SeedUser($"{token}a", "User");
        SeedUser($"{token}b", "User");
        SeedUser($"{token}c", "User");

        var result = await Service.SearchUsersAsync(new UserSearchRequestDto { Query = token, Skip = 1, Top = 1 });

        result.TotalCount.Should().Be(3);
        result.Items.Should().ContainSingle();
    }

    [Test]
    public async Task SearchUsers_ShouldOnlyReturnActiveUsers()
    {
        var token = $"Z{Guid.NewGuid():N}";
        var active = SeedUser($"{token}active", "User");
        SeedUser($"{token}inactive", "User", isActive: false);

        var result = await Service.SearchUsersAsync(new UserSearchRequestDto { Query = token, Skip = 0, Top = 50 });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Id.Should().Be(active.Id);
    }

    private TestUser SeedUser(string firstName, string lastName, bool isActive = true)
    {
        var user = new TestUser
        {
            UserName = $"user_{Guid.NewGuid():N}",
            Password = "x",
            Email = $"{Guid.NewGuid():N}@test.com",
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
        };
        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }
}
