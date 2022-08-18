namespace Cause.SecurityManagement.Tests.Services
{
    using Cause.SecurityManagement.Services;
    using TUser = Cause.SecurityManagement.Models.User;
    using System;
    using NUnit.Framework;
    using FluentAssertions;
    using NSubstitute;
    using System.Collections.Generic;
    using Cause.SecurityManagement.Models;
    using System.Linq;
    using Microsoft.Extensions.Options;
    using Cause.SecurityManagement.Models.Configuration;
    using Cause.SecurityManagement.Repositories;

    [TestFixture]
    public class BaseGroupManagementServiceTests
    {
        private BaseGroupManagementService<TUser> baseGroupManagementService;
        private ICurrentUserService currentUserService;
        private IUserManagementService<TUser> userManagementService;
        private IOptions<SecurityConfiguration> configuration;
        private IGroupRepository groupRepository;
        private IUserGroupRepository userGroupRepository;
        private IGroupPermissionRepository groupPermissionRepository;


        private readonly Group groupAAssignableByAllUsers = CreateGroup("Group A AssignableByAllUsers", true);
        private readonly Group groupBAssignableByAllUsers = CreateGroup("Group B AssignableByAllUsers", true);
        private readonly Group groupANotAssignableByAllUsers = CreateGroup("Group A Not AssignableByAllUsers", false);
        private readonly Group groupBNotAssignableByAllUsers = CreateGroup("Group B Not AssignableByAllUsers", false);

        private readonly SecurityConfiguration securityOptions = new SecurityConfiguration();

        [SetUp]
        public void SetUp()
        {
            currentUserService = Substitute.For<ICurrentUserService>();
            userManagementService = Substitute.For<IUserManagementService<TUser>>();
            configuration = Options.Create(securityOptions);
            groupRepository = Substitute.For<IGroupRepository>();
            userGroupRepository = Substitute.For<IUserGroupRepository>();
            groupPermissionRepository = Substitute.For<IGroupPermissionRepository>();
            baseGroupManagementService = new BaseGroupManagementService<TUser>(currentUserService, userManagementService, configuration, groupRepository, userGroupRepository, groupPermissionRepository);
        }

        [Test]
        public void ConfigRequiredPermissionForAllGroupsAccessIsEmpty_WhenGetActiveGroups_ShouldReturnsAllGroups()
        {
            securityOptions.RequiredPermissionForAllGroupsAccess = "";
            groupRepository.GetActiveGroups().Returns(new List<Group>{ groupAAssignableByAllUsers , groupBAssignableByAllUsers, groupANotAssignableByAllUsers, groupBNotAssignableByAllUsers });

            var result = baseGroupManagementService.GetActiveGroups();

            result.Should().BeEquivalentTo(new List<Group>
            {
                groupAAssignableByAllUsers, groupBAssignableByAllUsers, groupANotAssignableByAllUsers,
                groupBNotAssignableByAllUsers
            });
        }

        [Test]
        public void UserWithPermissionForAllGroupsAccess_WhenGetActiveGroups_ShouldReturnsAllGroups()
        {
            securityOptions.RequiredPermissionForAllGroupsAccess = "requiredPermission";
            userManagementService.HasPermission(Arg.Any<Guid>(), "requiredPermission").Returns(true);
            groupRepository.GetActiveGroups().Returns(new List<Group> { groupAAssignableByAllUsers, groupBAssignableByAllUsers, groupANotAssignableByAllUsers, groupBNotAssignableByAllUsers });

            var result = baseGroupManagementService.GetActiveGroups();

            result.Should().BeEquivalentTo(new List<Group>
            {
                groupAAssignableByAllUsers, groupBAssignableByAllUsers, groupANotAssignableByAllUsers,
                groupBNotAssignableByAllUsers
            });
        }

        [Test]
        public void UserWithoutPermissionForAllGroupsAccess_WhenGetActiveGroups_ShouldReturnsOnlyGroupsAssignableByAllUsers()
        {
            securityOptions.RequiredPermissionForAllGroupsAccess = "requiredPermission";
            userManagementService.HasPermission(Arg.Any<Guid>(), "requiredPermission").Returns(false);
            groupRepository.GetActiveGroups().Returns(new List<Group> { groupAAssignableByAllUsers, groupBAssignableByAllUsers, groupANotAssignableByAllUsers, groupBNotAssignableByAllUsers });

            var result = baseGroupManagementService.GetActiveGroups();

            result.Should().BeEquivalentTo(new List<Group>
            {
                groupAAssignableByAllUsers, groupBAssignableByAllUsers
            });
        }

        private static Group CreateGroup(string name, bool assignableByAllUsers)
        {
            return new Group { Name = name, AssignableByAllUsers = assignableByAllUsers };
        }
    }
}