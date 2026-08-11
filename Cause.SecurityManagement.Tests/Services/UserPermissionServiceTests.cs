namespace Cause.SecurityManagement.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AwesomeAssertions;
    using Cause.SecurityManagement.Core.Repositories;
    using Cause.SecurityManagement.Core.Services;
    using Cause.SecurityManagement.Models.DataTransferObjects;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class UserPermissionServiceTests
    {
        private const string SomeTag = "CanEditBuilding";

        private IUserPermissionRepository userPermissionRepository;
        private IGroupPermissionRepository groupPermissionRepository;
        private UserPermissionService service;
        private Guid someUserId;

        [SetUp]
        public void SetUp()
        {
            userPermissionRepository = Substitute.For<IUserPermissionRepository>();
            groupPermissionRepository = Substitute.For<IGroupPermissionRepository>();
            someUserId = Guid.NewGuid();

            userPermissionRepository.GetForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new List<UserMergedPermission>());
            groupPermissionRepository.GetForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new List<UserMergedPermission>());

            service = new UserPermissionService(groupPermissionRepository, userPermissionRepository);
        }

        private static UserMergedPermission PermissionFor(string tag, bool isAllowed)
            => new() { Access = isAllowed, FeatureName = tag };

        [Test]
        public async Task UserWithAllowedPermission_WhenHasPermissionAsync_ShouldReturnTrue()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: true)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Test]
        public async Task UserWithDeniedPermission_WhenHasPermissionAsync_ShouldReturnFalse()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: false)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Test]
        public async Task UserWithoutThePermission_WhenHasPermissionAsync_ShouldReturnFalse()
        {
            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Test]
        public async Task GroupPermissionOnly_WhenHasPermissionAsync_ShouldReturnTrue()
        {
            groupPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: true)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Test]
        public async Task GroupDenyAndUserAllow_WhenHasPermissionAsync_ShouldReturnFalseBecauseDenyWins()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: true)]);
            groupPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: false)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeFalse("PermissionMergeTool computes Access with All(), so a deny wins");
        }

        [Test]
        public async Task UserDenyAndGroupAllow_WhenHasPermissionAsync_ShouldReturnFalseBecauseDenyWins()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: false)]);
            groupPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: true)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeFalse("deny wins from either source, regardless of argument order");
        }

        [Test]
        public async Task UserAndGroupPermissions_WhenGetPermissionsForUserAsync_ShouldMergeBoth()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor("FromUser", isAllowed: true)]);
            groupPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor("FromGroup", isAllowed: true)]);

            var result = await service.GetPermissionsForUserAsync(someUserId, CancellationToken.None);

            result.Should().BeEquivalentTo([
                PermissionFor("FromUser", isAllowed: true),
                PermissionFor("FromGroup", isAllowed: true)]);
        }

        [Test]
        public async Task BothRepositories_WhenGetPermissionsForUserAsync_ShouldReceiveTheCancellationToken()
        {
            using var cancellation = new CancellationTokenSource();

            await service.GetPermissionsForUserAsync(someUserId, cancellation.Token);

            await userPermissionRepository.Received(1).GetForUserAsync(someUserId, cancellation.Token);
            await groupPermissionRepository.Received(1).GetForUserAsync(someUserId, cancellation.Token);
        }

        [Test]
        public async Task RepositoryThrowsOperationCanceled_WhenHasPermissionAsync_ShouldPropagate()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns<List<UserMergedPermission>>(_ => throw new OperationCanceledException());

            var act = async () => await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task ImplementationWithoutAsyncOverrides_WhenHasPermissionAsync_ShouldDelegateToTheSynchronousMember()
        {
            IUserPermissionService synchronousOnly = new SynchronousOnlyPermissionService();

            var result = await synchronousOnly.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeTrue("the default interface implementation must delegate to HasPermission");
        }

        [Test]
        public async Task ImplementationWithoutAsyncOverrides_WhenHasPermissionAsyncForAnUnheldTag_ShouldReturnFalse()
        {
            IUserPermissionService synchronousOnly = new SynchronousOnlyPermissionService();

            var result = await synchronousOnly.HasPermissionAsync(someUserId, "SomeOtherTag", CancellationToken.None);

            result.Should().BeFalse();
        }

        [Test]
        public async Task ImplementationWithoutAsyncOverrides_WhenGetPermissionsForUserAsync_ShouldDelegateToTheSynchronousMember()
        {
            IUserPermissionService synchronousOnly = new SynchronousOnlyPermissionService();

            var result = await synchronousOnly.GetPermissionsForUserAsync(someUserId, CancellationToken.None);

            result.Should().BeEquivalentTo([PermissionFor(SomeTag, isAllowed: true)]);
        }

        private sealed class SynchronousOnlyPermissionService : IUserPermissionService
        {
            public bool HasPermission(Guid userId, string permissionTag) => permissionTag == SomeTag;

            public List<UserMergedPermission> GetPermissionsForUser(Guid userId)
                => [PermissionFor(SomeTag, isAllowed: true)];
        }
    }
}
