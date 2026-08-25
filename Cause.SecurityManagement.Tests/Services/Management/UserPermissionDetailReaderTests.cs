using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services.Management
{
    [TestFixture]
    public class UserPermissionDetailReaderTests
    {
        private const string SomeTag = "CanEditBuilding";
        private const string AnotherTag = "CanAccessUsers";

        private TestPermissionContext context;
        private UserPermissionDetailReader<TestPermissionUser> reader;
        private Guid someUserId;
        private Guid somePermissionId;
        private Guid anotherPermissionId;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestPermissionContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            context = new TestPermissionContext(options);
            reader = new UserPermissionDetailReader<TestPermissionUser>(context);
            someUserId = Guid.NewGuid();
            somePermissionId = Guid.NewGuid();
            anotherPermissionId = Guid.NewGuid();
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await context.DisposeAsync();
        }

        [Test]
        public async Task NoAssignmentAnywhere_WhenGetForUser_ShouldReportUndefinedWithoutOrigin()
        {
            await GivenCatalogAsync();

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            var detail = details.Find(permission => permission.Tag == SomeTag);
            detail.Status.Should().Be(PermissionStatus.Undefined);
            detail.Origins.Should().BeEmpty();
        }

        [Test]
        public async Task OnlyGroupAllows_WhenGetForUser_ShouldReportAllowedWithTheGroupOrigin()
        {
            await GivenCatalogAsync();
            var groupId = await GivenGroupMembershipAsync("Dispatchers");
            await GivenGroupPermissionAsync(groupId, somePermissionId, isAllowed: true);

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            var detail = details.Find(permission => permission.Tag == SomeTag);
            detail.Status.Should().Be(PermissionStatus.Allowed);
            detail.Origins.Should().HaveCount(1);
            detail.Origins[0].Kind.Should().Be(UserPermissionOriginKind.Group);
            detail.Origins[0].GroupId.Should().Be(groupId);
            detail.Origins[0].GroupName.Should().Be("Dispatchers");
            detail.Origins[0].IsAllowed.Should().BeTrue();
        }

        [Test]
        public async Task OnlyGroupDenies_WhenGetForUser_ShouldReportDenied()
        {
            await GivenCatalogAsync();
            var groupId = await GivenGroupMembershipAsync("Dispatchers");
            await GivenGroupPermissionAsync(groupId, somePermissionId, isAllowed: false);

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            details.Find(permission => permission.Tag == SomeTag).Status.Should().Be(PermissionStatus.Denied);
        }

        [Test]
        public async Task OnlyUserOverrideAllows_WhenGetForUser_ShouldReportAllowedWithTheOverrideOrigin()
        {
            await GivenCatalogAsync();
            await GivenUserPermissionAsync(somePermissionId, isAllowed: true);

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            var detail = details.Find(permission => permission.Tag == SomeTag);
            detail.Status.Should().Be(PermissionStatus.Allowed);
            detail.Origins.Should().HaveCount(1);
            detail.Origins[0].Kind.Should().Be(UserPermissionOriginKind.UserOverride);
            detail.Origins[0].GroupId.Should().BeNull();
        }

        [Test]
        public async Task GroupAllowsAndUserOverrideDenies_WhenGetForUser_ShouldReportDenied()
        {
            await GivenCatalogAsync();
            var groupId = await GivenGroupMembershipAsync("Dispatchers");
            await GivenGroupPermissionAsync(groupId, somePermissionId, isAllowed: true);
            await GivenUserPermissionAsync(somePermissionId, isAllowed: false);

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            var detail = details.Find(permission => permission.Tag == SomeTag);
            detail.Status.Should().Be(PermissionStatus.Denied);
            detail.Origins.Should().HaveCount(2);
        }

        [Test]
        public async Task GroupDeniesAndUserOverrideAllows_WhenGetForUser_ShouldStillReportDenied()
        {
            await GivenCatalogAsync();
            var groupId = await GivenGroupMembershipAsync("Dispatchers");
            await GivenGroupPermissionAsync(groupId, somePermissionId, isAllowed: false);
            await GivenUserPermissionAsync(somePermissionId, isAllowed: true);

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            details.Find(permission => permission.Tag == SomeTag).Status.Should().Be(PermissionStatus.Denied);
        }

        [Test]
        public async Task TwoGroupsDisagree_WhenGetForUser_ShouldReportDenied()
        {
            await GivenCatalogAsync();
            var allowingGroupId = await GivenGroupMembershipAsync("Dispatchers");
            var denyingGroupId = await GivenGroupMembershipAsync("Trainees");
            await GivenGroupPermissionAsync(allowingGroupId, somePermissionId, isAllowed: true);
            await GivenGroupPermissionAsync(denyingGroupId, somePermissionId, isAllowed: false);

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            details.Find(permission => permission.Tag == SomeTag).Status.Should().Be(PermissionStatus.Denied);
        }

        [Test]
        public async Task ACatalogOfSeveralPermissions_WhenGetForUser_ShouldReturnThemAllOrderedBySequence()
        {
            await GivenCatalogAsync();

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            details.Should().HaveCount(2);
            details[0].Tag.Should().Be(SomeTag);
            details[1].Tag.Should().Be(AnotherTag);
        }

        [Test]
        public async Task PermissionsSharingASequence_WhenGetForUser_ShouldBeOrderedByName()
        {
            context.ModulePermissions.AddRange(
                new ModulePermission { Id = Guid.NewGuid(), Tag = "third", Name = "Schedule - delete", Sequence = 5 },
                new ModulePermission { Id = Guid.NewGuid(), Tag = "first", Name = "Buildings - access", Sequence = 5 },
                new ModulePermission { Id = Guid.NewGuid(), Tag = "second", Name = "Messaging - access", Sequence = 5 }
            );
            await context.SaveChangesAsync();

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            details.ConvertAll(permission => permission.Name).Should().ContainInOrder(
                "Buildings - access", "Messaging - access", "Schedule - delete");
        }

        [Test]
        public async Task AnotherUserHoldsThePermission_WhenGetForUser_ShouldIgnoreIt()
        {
            await GivenCatalogAsync();
            var groupId = await GivenGroupMembershipAsync("Dispatchers", Guid.NewGuid());
            await GivenGroupPermissionAsync(groupId, somePermissionId, isAllowed: true);
            context.UserPermissions.Add(new UserPermission
            {
                Id = Guid.NewGuid(),
                IdUser = Guid.NewGuid(),
                IdModulePermission = somePermissionId,
                IsAllowed = true,
            });
            await context.SaveChangesAsync();

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            var detail = details.Find(permission => permission.Tag == SomeTag);
            detail.Status.Should().Be(PermissionStatus.Undefined);
            detail.Origins.Should().BeEmpty();
        }

        [Test]
        public async Task ACatalogEntry_WhenGetForUser_ShouldCarryItsIdentityAndLabel()
        {
            await GivenCatalogAsync();

            var details = await reader.GetForUserAsync(someUserId, CancellationToken.None);

            var detail = details.Find(permission => permission.Tag == SomeTag);
            detail.IdModulePermission.Should().Be(somePermissionId);
            detail.Name.Should().Be("Edit a building");
        }

        private async Task GivenCatalogAsync()
        {
            context.ModulePermissions.AddRange(
                new ModulePermission { Id = somePermissionId, Tag = SomeTag, Name = "Edit a building", Sequence = 1 },
                new ModulePermission { Id = anotherPermissionId, Tag = AnotherTag, Name = "Access users", Sequence = 2 }
            );
            await context.SaveChangesAsync();
        }

        private async Task<Guid> GivenGroupMembershipAsync(string groupName, Guid? memberId = null)
        {
            var groupId = Guid.NewGuid();
            context.Groups.Add(new Group { Id = groupId, Name = groupName });
            context.UserGroups.Add(new UserGroup
            {
                Id = Guid.NewGuid(),
                IdGroup = groupId,
                IdUser = memberId ?? someUserId,
            });
            await context.SaveChangesAsync();
            return groupId;
        }

        private async Task GivenGroupPermissionAsync(Guid groupId, Guid permissionId, bool isAllowed)
        {
            context.GroupPermissions.Add(new GroupPermission
            {
                Id = Guid.NewGuid(),
                IdGroup = groupId,
                IdModulePermission = permissionId,
                IsAllowed = isAllowed,
            });
            await context.SaveChangesAsync();
        }

        private async Task GivenUserPermissionAsync(Guid permissionId, bool isAllowed)
        {
            context.UserPermissions.Add(new UserPermission
            {
                Id = Guid.NewGuid(),
                IdUser = someUserId,
                IdModulePermission = permissionId,
                IsAllowed = isAllowed,
            });
            await context.SaveChangesAsync();
        }

        private sealed class TestPermissionUser : User { }

        private sealed class TestPermissionContext(DbContextOptions<TestPermissionContext> options)
            : BaseSecurityContext<TestPermissionUser>(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                AddSecurityManagementMappings(modelBuilder);
            }
        }
    }
}
