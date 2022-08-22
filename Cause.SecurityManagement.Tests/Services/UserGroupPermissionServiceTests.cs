namespace Cause.SecurityManagement.Tests.Services
{
    using Cause.SecurityManagement.Services;
    using System;
    using NUnit.Framework;
    using FluentAssertions;
    using NSubstitute;
    using Cause.SecurityManagement.Models;
    using Cause.SecurityManagement.Repositories;
    using Microsoft.Extensions.Options;
    using Cause.SecurityManagement.Models.Configuration;

    [TestFixture]
    public class UserGroupPermissionServiceTests
    {
        private UserGroupPermissionService userGroupPermissionService;
        private ICurrentUserService currentUserService;
        private IGroupRepository groupRepository;
        private IUserPermissionService userPermissionService;
        private IOptions<SecurityConfiguration> configuration;

        private readonly SecurityConfiguration securityConfiguration = new();
        private static readonly string RequiredPermission = "requiredPermission";

        [SetUp]
        public void SetUp()
        {
            currentUserService = Substitute.For<ICurrentUserService>();
            userPermissionService = Substitute.For<IUserPermissionService>();
            groupRepository = Substitute.For<IGroupRepository>();
            configuration = Options.Create(securityConfiguration);
            securityConfiguration.RequiredPermissionForAllGroupsAccess = RequiredPermission;

            userGroupPermissionService = new UserGroupPermissionService(currentUserService, groupRepository, userPermissionService, configuration);
        }

        [Test]
        public void ConfigRequiredPermissionForAllGroupsAccessIsMissing_WhenCurrentUserHasRequiredPermissionForAllGroupsAccess_ShouldSucceed()
        {
            securityConfiguration.RequiredPermissionForAllGroupsAccess = "";

            var result = userGroupPermissionService.CurrentUserHasRequiredPermissionForAllGroupsAccess();
            
            result.Should().BeTrue();
        }

        [Test]
        public void CurrentUserWithPermissionForAllGroupsAccess_WhenCurrentUserHasRequiredPermissionForAllGroupsAccess_ShouldSucceed()
        {
            userPermissionService.HasPermission(Arg.Any<Guid>(), RequiredPermission).Returns(true);

            var result = userGroupPermissionService.CurrentUserHasRequiredPermissionForAllGroupsAccess();
            
            result.Should().BeTrue();
        }

        [Test]
        public void UserWithoutPermissionForAllGroupsAccess_WhenCurrentUserHasRequiredPermissionForAllGroupsAccess_ShouldFail()
        {
            userPermissionService.HasPermission(Arg.Any<Guid>(), RequiredPermission).Returns(false);

            var result = userGroupPermissionService.CurrentUserHasRequiredPermissionForAllGroupsAccess();

            result.Should().BeFalse();
        }

        [Test]
        public void UserWithoutPermissionForAllGroupsAccessAndAGroupAssignableByAllUsers_WhenCurrentUserHasRequiredPermissionForGroupsAccess_ShouldSucceed()
        {
            userPermissionService.HasPermission(Arg.Any<Guid>(), RequiredPermission).Returns(false);
            var groupAssignableByAllUser = new Group { AssignableByAllUsers = true };

            var result = userGroupPermissionService.CurrentUserHasRequiredPermissionForGroupsAccess(groupAssignableByAllUser);

            result.Should().BeTrue();
        }

        [Test]
        public void UserWithoutPermissionForAllGroupsAccessAndAGroupNotAssignableByAllUsers_WhenCurrentUserHasRequiredPermissionForGroupsAccess_ShouldFail()
        {
            userPermissionService.HasPermission(Arg.Any<Guid>(), RequiredPermission).Returns(false);
            var groupAssignableByAllUser = new Group { AssignableByAllUsers = false };

            var result = userGroupPermissionService.CurrentUserHasRequiredPermissionForGroupsAccess(groupAssignableByAllUser);

            result.Should().BeFalse();
        }
    }
}