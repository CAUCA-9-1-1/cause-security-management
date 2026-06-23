using System;
using System.Linq.Expressions;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Management;

[TestFixture]
public class AdditionalInformation_DefaultProvider_Tests : IntegrationTestBase
{
    private IUserSearchService UserSearch => Resolve<IUserSearchService>();
    private IGroupManagementApiService GroupManagement => Resolve<IGroupManagementApiService>();

    [Test]
    public async Task UserSearch_WithDefaultProvider_AdditionalInformationIsNull()
    {
        var user = SeedUser("Alice", "Default");

        var result = await UserSearch.SearchUsersAsync(new UserSearchRequestDto
        {
            Query = "Default",
            Skip = 0,
            Top = 10,
        });

        result.Items.Should().ContainSingle().Which.AdditionalInformation.Should().BeNull();
    }

    [Test]
    public async Task GroupManagement_WithDefaultProvider_AdditionalInformationIsNull()
    {
        var groupId = Guid.NewGuid();
        var user = SeedUser("Bob", "DefaultGroup");
        SeedGroup(groupId, "TestGroupDefault", user.Id);

        var group = await GroupManagement.GetGroupAsync(groupId);

        group.Users.Should().ContainSingle().Which.AdditionalInformation.Should().BeNull();
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

    private void SeedGroup(Guid groupId, string name, Guid userId)
    {
        Context.Groups.Add(new Group { Id = groupId, Name = name });
        Context.UserGroups.Add(new UserGroup { Id = Guid.NewGuid(), IdGroup = groupId, IdUser = userId });
        Context.SaveChanges();
    }
}

[TestFixture]
public class AdditionalInformation_CustomProvider_Tests : IntegrationTestBase
{
    private IUserSearchService UserSearch => Resolve<IUserSearchService>();
    private IGroupManagementApiService GroupManagement => Resolve<IGroupManagementApiService>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddScoped<IUserAdditionalInformationProvider<TestUser>, EmailAdditionalInformationProvider>();
    }

    [Test]
    public async Task UserSearch_WithCustomProvider_AdditionalInformationIsPopulatedFromQuery()
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var user = SeedUser("Carol", "Custom", email);

        var result = await UserSearch.SearchUsersAsync(new UserSearchRequestDto
        {
            Query = "Custom",
            Skip = 0,
            Top = 10,
        });

        result.Items.Should().ContainSingle().Which.AdditionalInformation.Should().Be(email);
    }

    [Test]
    public async Task GroupManagement_WithCustomProvider_AdditionalInformationIsPopulatedFromQuery()
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var groupId = Guid.NewGuid();
        var user = SeedUser("Dave", "CustomGroup", email);
        SeedGroup(groupId, $"TestGroupCustom_{Guid.NewGuid():N}", user.Id);

        var group = await GroupManagement.GetGroupAsync(groupId);

        group.Users.Should().ContainSingle().Which.AdditionalInformation.Should().Be(email);
    }

    private TestUser SeedUser(string firstName, string lastName, string email, bool isActive = true)
    {
        var user = new TestUser
        {
            UserName = $"user_{Guid.NewGuid():N}",
            Password = "x",
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
        };
        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    private void SeedGroup(Guid groupId, string name, Guid userId)
    {
        Context.Groups.Add(new Group { Id = groupId, Name = name });
        Context.UserGroups.Add(new UserGroup { Id = Guid.NewGuid(), IdGroup = groupId, IdUser = userId });
        Context.SaveChanges();
    }

    private sealed class EmailAdditionalInformationProvider : IUserAdditionalInformationProvider<TestUser>
    {
        public Expression<Func<TestUser, string>> GetAdditionalInformation() => user => user.Email;
    }
}
