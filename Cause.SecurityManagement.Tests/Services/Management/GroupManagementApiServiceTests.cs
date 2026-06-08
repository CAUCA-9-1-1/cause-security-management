using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services.Management
{
    [TestFixture]
    public class GroupManagementApiServiceTests
    {
        private TestGroupContext context;
        private GroupManagementApiService<TestGroupUser> service;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestGroupContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            context = new TestGroupContext(options);
            service = new GroupManagementApiService<TestGroupUser>(context);
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await context.DisposeAsync();
        }

        [Test]
        public async Task NoGroupWithThatName_IsGroupNameAvailable_ShouldReturnTrue()
        {
            var isAvailable = await service.IsGroupNameAvailableAsync("Dispatchers", null, CancellationToken.None);

            isAvailable.Should().BeTrue();
        }

        [Test]
        public async Task AnotherGroupHasThatName_IsGroupNameAvailable_ShouldReturnFalse()
        {
            context.Groups.Add(new Group { Id = Guid.NewGuid(), Name = "Dispatchers" });
            await context.SaveChangesAsync();

            var isAvailable = await service.IsGroupNameAvailableAsync("dispatchers", null, CancellationToken.None);

            isAvailable.Should().BeFalse();
        }

        [Test]
        public async Task AnotherGroupHasThatNameButExcludeIdIsItsOwnId_IsGroupNameAvailable_ShouldReturnTrue()
        {
            var groupId = Guid.NewGuid();
            context.Groups.Add(new Group { Id = groupId, Name = "Dispatchers" });
            await context.SaveChangesAsync();

            var isAvailable = await service.IsGroupNameAvailableAsync("Dispatchers", groupId, CancellationToken.None);

            isAvailable.Should().BeTrue();
        }

        [Test]
        public async Task NullName_IsGroupNameAvailable_ShouldReturnFalse()
        {
            var isAvailable = await service.IsGroupNameAvailableAsync(null, null, CancellationToken.None);

            isAvailable.Should().BeFalse();
        }

        [Test]
        public async Task WhitespaceName_IsGroupNameAvailable_ShouldReturnFalse()
        {
            var isAvailable = await service.IsGroupNameAvailableAsync("   ", null, CancellationToken.None);

            isAvailable.Should().BeFalse();
        }

        [Test]
        public async Task GroupHasActiveAndInactiveMember_GetGroup_ShouldReturnOnlyActiveMember()
        {
            var groupId = Guid.NewGuid();
            var activeUserId = Guid.NewGuid();
            var inactiveUserId = Guid.NewGuid();
            context.Groups.Add(new Group { Id = groupId, Name = "Test Group" });
            context.Users.AddRange(
                new TestGroupUser { Id = activeUserId, FirstName = "Active", LastName = "User", UserName = "active", Email = "active@test.com", Password = "x", IsActive = true },
                new TestGroupUser { Id = inactiveUserId, FirstName = "Inactive", LastName = "User", UserName = "inactive", Email = "inactive@test.com", Password = "x", IsActive = false }
            );
            context.UserGroups.AddRange(
                new UserGroup { Id = Guid.NewGuid(), IdGroup = groupId, IdUser = activeUserId },
                new UserGroup { Id = Guid.NewGuid(), IdGroup = groupId, IdUser = inactiveUserId }
            );
            await context.SaveChangesAsync();

            var group = await service.GetGroupAsync(groupId, CancellationToken.None);

            group.Users.Should().HaveCount(1);
            group.Users[0].Id.Should().Be(activeUserId);
        }

        private sealed class TestGroupUser : User { }

        private sealed class TestGroupContext(DbContextOptions<TestGroupContext> options)
            : BaseSecurityContext<TestGroupUser>(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                AddSecurityManagementMappings(modelBuilder);
            }
        }
    }
}
