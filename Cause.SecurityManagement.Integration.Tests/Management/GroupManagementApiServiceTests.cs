using AwesomeAssertions;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Management;

[TestFixture]
public class GroupManagementApiServiceTests : IntegrationTestBase
{
    private IGroupManagementApiService Service => Resolve<IGroupManagementApiService>();

    [Test]
    public async Task WhenGroupIsNew_SaveGroup_ShouldInsertGroupWithPermissionsAndMembers()
    {
        var user = SeedUser("Ada", "Lovelace");
        var permission = SeedModulePermission();
        var group = new GroupDto
        {
            Id = Guid.NewGuid(),
            Name = $"Group_{Guid.NewGuid():N}",
            AssignableByAllUsers = true,
            Permissions = [new GroupPermissionDto { Id = Guid.NewGuid(), IdModulePermission = permission.Id, IsAllowed = true }],
            Users = [new GroupUserDto { Id = user.Id }],
        };

        var saved = await Service.SaveGroupAsync(group);

        saved.Name.Should().Be(group.Name);
        saved.AssignableByAllUsers.Should().BeTrue();
        saved.Permissions.Should().ContainSingle().Which.IdModulePermission.Should().Be(permission.Id);
        saved.Users.Should().ContainSingle().Which.FullName.Should().Be("Ada Lovelace");
        (await Context.Groups.FindAsync(group.Id)).Should().NotBeNull();
    }

    [Test]
    public async Task WhenGroupExists_SaveGroup_ShouldUpdateNameAndReconcileChildren()
    {
        var keptUser = SeedUser("Grace", "Hopper");
        var droppedUser = SeedUser("Alan", "Turing");
        var addedUser = SeedUser("Edsger", "Dijkstra");
        var keptPermission = SeedModulePermission();
        var droppedPermission = SeedModulePermission();
        var addedPermission = SeedModulePermission();

        var groupId = Guid.NewGuid();
        var keptPermissionId = Guid.NewGuid();
        await Service.SaveGroupAsync(new GroupDto
        {
            Id = groupId,
            Name = "Original",
            Permissions =
            [
                new GroupPermissionDto { Id = keptPermissionId, IdModulePermission = keptPermission.Id, IsAllowed = true },
                new GroupPermissionDto { Id = Guid.NewGuid(), IdModulePermission = droppedPermission.Id, IsAllowed = true },
            ],
            Users = [new GroupUserDto { Id = keptUser.Id }, new GroupUserDto { Id = droppedUser.Id }],
        });

        var updated = await Service.SaveGroupAsync(new GroupDto
        {
            Id = groupId,
            Name = "Renamed",
            Permissions =
            [
                new GroupPermissionDto { Id = keptPermissionId, IdModulePermission = keptPermission.Id, IsAllowed = false },
                new GroupPermissionDto { Id = Guid.NewGuid(), IdModulePermission = addedPermission.Id, IsAllowed = true },
            ],
            Users = [new GroupUserDto { Id = keptUser.Id }, new GroupUserDto { Id = addedUser.Id }],
        });

        updated.Name.Should().Be("Renamed");
        updated.Permissions.Should().HaveCount(2);
        updated.Permissions.Should().Contain(p => p.IdModulePermission == keptPermission.Id && !p.IsAllowed);
        updated.Permissions.Should().Contain(p => p.IdModulePermission == addedPermission.Id);
        updated.Permissions.Should().NotContain(p => p.IdModulePermission == droppedPermission.Id);
        updated.Users.Select(u => u.Id).Should().BeEquivalentTo([keptUser.Id, addedUser.Id]);
    }

    [Test]
    public async Task WhenGroupExists_DeleteGroup_ShouldRemoveGroupAndDependents()
    {
        var user = SeedUser("Linus", "Torvalds");
        var permission = SeedModulePermission();
        var groupId = Guid.NewGuid();
        await Service.SaveGroupAsync(new GroupDto
        {
            Id = groupId,
            Name = $"Group_{Guid.NewGuid():N}",
            Permissions = [new GroupPermissionDto { Id = Guid.NewGuid(), IdModulePermission = permission.Id, IsAllowed = true }],
            Users = [new GroupUserDto { Id = user.Id }],
        });

        var deleted = await Service.DeleteGroupAsync(groupId);

        deleted.Should().BeTrue();
        (await Context.Groups.FindAsync(groupId)).Should().BeNull();
        Context.GroupPermissions.Any(p => p.IdGroup == groupId).Should().BeFalse();
        Context.UserGroups.Any(u => u.IdGroup == groupId).Should().BeFalse();
    }

    [Test]
    public async Task WhenGroupDoesNotExist_DeleteGroup_ShouldReturnFalse()
    {
        (await Service.DeleteGroupAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Test]
    public async Task WhenGroupDoesNotExist_GetGroup_ShouldReturnNull()
    {
        (await Service.GetGroupAsync(Guid.NewGuid())).Should().BeNull();
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
}
