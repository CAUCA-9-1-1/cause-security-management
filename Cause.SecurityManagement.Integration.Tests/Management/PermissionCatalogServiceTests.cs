using AwesomeAssertions;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Models;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Management;

[TestFixture]
public class PermissionCatalogServiceTests : IntegrationTestBase
{
    private IPermissionCatalogService Service => Resolve<IPermissionCatalogService>();

    [Test]
    public async Task GetPermissions_ShouldMapModulePermissionsAndOrderBySequence()
    {
        var token = $"tag_{Guid.NewGuid():N}";
        var second = SeedPermission($"{token}_b", "Second permission", sequence: 2);
        var first = SeedPermission($"{token}_a", "First permission", sequence: 1);

        var catalog = (await Service.GetPermissionsAsync()).Where(permission => permission.Tag.StartsWith(token)).ToList();

        catalog.Should().HaveCount(2);
        catalog[0].Tag.Should().Be(first.Tag);
        catalog[1].Tag.Should().Be(second.Tag);
        catalog[0].Id.Should().Be(first.Id);
        catalog[0].IdModulePermission.Should().Be(first.Id);
        catalog[0].Name.Should().Be("First permission");
    }

    private ModulePermission SeedPermission(string tag, string name, int sequence)
    {
        var module = new Module { Id = Guid.NewGuid(), Name = $"module_{Guid.NewGuid():N}", Tag = $"mod_{Guid.NewGuid():N}" };
        Context.Modules.Add(module);
        var permission = new ModulePermission
        {
            Id = Guid.NewGuid(),
            IdModule = module.Id,
            Tag = tag,
            Name = name,
            Sequence = sequence,
        };
        Context.ModulePermissions.Add(permission);
        Context.SaveChanges();
        return permission;
    }
}
