using AwesomeAssertions;
using Cause.SecurityManagement.Core.Repositories;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Models;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Repositories;

[TestFixture]
public class GroupPermissionRepositoryTests : IntegrationTestBase
{
    private IGroupPermissionRepository Repository => Resolve<IGroupPermissionRepository>();

    [Test]
    public async Task WhenUserBelongsToTwoGroups_GetForUserAsync_ShouldReturnBothGroupsPermissionsWithTagAndAccess()
    {
        var user = SeedUser();
        var firstPermission = SeedModulePermission();
        var secondPermission = SeedModulePermission();
        var firstGroup = SeedGroup();
        var secondGroup = SeedGroup();
        SeedGroupPermission(firstGroup, firstPermission, isAllowed: true);
        SeedGroupPermission(secondGroup, secondPermission, isAllowed: false);
        SeedUserGroup(user, firstGroup);
        SeedUserGroup(user, secondGroup);

        var permissions = await Repository.GetForUserAsync(user.Id, CancellationToken.None);

        permissions.Should().HaveCount(2);
        permissions.Should().Contain(permission => permission.FeatureName == firstPermission.Tag && permission.Access);
        permissions.Should().Contain(permission => permission.FeatureName == secondPermission.Tag && !permission.Access);
    }

    private TestUser SeedUser()
    {
        var user = new TestUser
        {
            UserName = $"user_{Guid.NewGuid():N}",
            Password = "x",
            Email = $"{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
        };
        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    private Group SeedGroup()
    {
        var group = new Group { Id = Guid.NewGuid(), Name = $"group_{Guid.NewGuid():N}" };
        Context.Groups.Add(group);
        Context.SaveChanges();
        return group;
    }

    private ModulePermission SeedModulePermission()
    {
        var module = new Module { Id = Guid.NewGuid(), Name = $"module_{Guid.NewGuid():N}", Tag = $"mod_{Guid.NewGuid():N}" };
        Context.Modules.Add(module);
        var permission = new ModulePermission
        {
            Id = Guid.NewGuid(),
            IdModule = module.Id,
            Tag = $"tag_{Guid.NewGuid():N}",
            Name = $"name_{Guid.NewGuid():N}",
        };
        Context.ModulePermissions.Add(permission);
        Context.SaveChanges();
        return permission;
    }

    private void SeedGroupPermission(Group group, ModulePermission permission, bool isAllowed)
    {
        Context.GroupPermissions.Add(new GroupPermission
        {
            Id = Guid.NewGuid(),
            IdGroup = group.Id,
            IdModulePermission = permission.Id,
            IsAllowed = isAllowed,
        });
        Context.SaveChanges();
    }

    private void SeedUserGroup(TestUser user, Group group)
    {
        Context.UserGroups.Add(new UserGroup
        {
            Id = Guid.NewGuid(),
            IdUser = user.Id,
            IdGroup = group.Id,
        });
        Context.SaveChanges();
    }
}
