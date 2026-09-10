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
    public class UserNameAvailabilityReaderTests
    {
        private static readonly Guid TakenUserId = Guid.NewGuid();
        private static readonly Guid OtherUserId = Guid.NewGuid();

        private TestUserContext context;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestUserContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            context = new TestUserContext(options);
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await context.DisposeAsync();
        }

        [Test]
        public async Task WhenAnotherActiveUserHasTheName_ShouldNotBeAvailable()
        {
            var reader = await CreateReaderWithAsync(ActiveUser(TakenUserId, "jtremblay"));

            var available = await reader.IsAvailableAsync("jtremblay", OtherUserId, CancellationToken.None);

            available.Should().BeFalse();
        }

        [Test]
        public async Task WhenOnlyADeactivatedUserHasTheName_ShouldBeAvailable()
        {
            var reader = await CreateReaderWithAsync(DeactivatedUser(TakenUserId, "jtremblay"));

            var available = await reader.IsAvailableAsync("jtremblay", OtherUserId, CancellationToken.None);

            available.Should().BeTrue();
        }

        [Test]
        public async Task WhenTheNameBelongsToTheUserBeingEdited_ShouldBeAvailable()
        {
            var reader = await CreateReaderWithAsync(ActiveUser(TakenUserId, "jtremblay"));

            var available = await reader.IsAvailableAsync("jtremblay", TakenUserId, CancellationToken.None);

            available.Should().BeTrue();
        }

        [Test]
        public async Task WhenNoUserHasTheName_ShouldBeAvailable()
        {
            var reader = await CreateReaderWithAsync(ActiveUser(TakenUserId, "mgagnon"));

            var available = await reader.IsAvailableAsync("jtremblay", OtherUserId, CancellationToken.None);

            available.Should().BeTrue();
        }

        [Test]
        public async Task WhenTheNameIsEmpty_ShouldNotBeAvailable()
        {
            var reader = await CreateReaderWithAsync(ActiveUser(TakenUserId, "jtremblay"));

            var available = await reader.IsAvailableAsync("   ", OtherUserId, CancellationToken.None);

            available.Should().BeFalse();
        }

        [Test]
        public async Task WhenCreatingWithNoUserToIgnore_ShouldNotBeAvailableIfAnActiveUserHasTheName()
        {
            var reader = await CreateReaderWithAsync(ActiveUser(TakenUserId, "jtremblay"));

            var available = await reader.IsAvailableAsync("jtremblay", Guid.Empty, CancellationToken.None);

            available.Should().BeFalse();
        }

        private async Task<UserNameAvailabilityReader<TestUser>> CreateReaderWithAsync(TestUser user)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return new UserNameAvailabilityReader<TestUser>(context);
        }

        private static TestUser ActiveUser(Guid id, string userName) => new()
        {
            Id = id,
            UserName = userName,
            FirstName = "Jean",
            LastName = "Tremblay",
            Email = $"{userName}@cauca.ca",
            Password = "hashed",
            IsActive = true,
        };

        private static TestUser DeactivatedUser(Guid id, string userName) => new()
        {
            Id = id,
            UserName = userName,
            FirstName = "Jean",
            LastName = "Tremblay",
            Email = $"{userName}@cauca.ca",
            Password = "hashed",
            IsActive = false,
        };

        private sealed class TestUser : User { }

        private sealed class TestUserContext(DbContextOptions<TestUserContext> options)
            : BaseSecurityContext<TestUser>(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                AddSecurityManagementMappings(modelBuilder);
            }
        }
    }
}
