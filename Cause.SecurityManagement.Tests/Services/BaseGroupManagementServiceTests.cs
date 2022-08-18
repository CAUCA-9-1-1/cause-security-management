namespace Cause.SecurityManagement.Tests.Services
{
    using Cause.SecurityManagement.Services;
    using NUnit.Framework;
    using FluentAssertions;
    using NSubstitute;
    using System.Collections.Generic;
    using Cause.SecurityManagement.Models;
    using Cause.SecurityManagement.Repositories;

    [TestFixture]
    public class BaseGroupManagementServiceTests
    {
        private BaseGroupManagementService baseGroupManagementService;
        private IGroupRepository groupRepository;
        private IUserGroupRepository userGroupRepository;
        private IGroupPermissionRepository groupPermissionRepository;
        private IUserGroupPermissionService userGroupPermissionService;

        private readonly Group groupAssignableByAllUsers = CreateGroup("Group AssignableByAllUsers", true);
        private readonly Group groupNotAssignableByAllUsers = CreateGroup("Group Not AssignableByAllUsers", false);

        [SetUp]
        public void SetUp()
        {
            groupRepository = Substitute.For<IGroupRepository>();
            userGroupRepository = Substitute.For<IUserGroupRepository>();
            groupPermissionRepository = Substitute.For<IGroupPermissionRepository>();
            userGroupPermissionService = Substitute.For<IUserGroupPermissionService>();
            baseGroupManagementService = new BaseGroupManagementService(groupRepository, userGroupRepository, groupPermissionRepository, userGroupPermissionService);
        }

        [Test]
        public void WhenGetActiveGroups_ShouldReturnsOnlyAssigableGroups()
        {
            userGroupPermissionService
                .CurrentUserHasRequiredPermissionForGroupsAccess(Arg.Is<Group>(groupAssignableByAllUsers))
                .Returns(true);
            userGroupPermissionService
                .CurrentUserHasRequiredPermissionForGroupsAccess(Arg.Is<Group>(groupNotAssignableByAllUsers))
                .Returns(false);
            groupRepository.GetActiveGroups().Returns(new List<Group>{ groupAssignableByAllUsers , groupNotAssignableByAllUsers });

            var result = baseGroupManagementService.GetActiveGroups();

            result.Should().BeEquivalentTo(new List<Group>
            {
                groupAssignableByAllUsers
            });
        }

        private static Group CreateGroup(string name, bool assignableByAllUsers)
        {
            return new Group { Name = name, AssignableByAllUsers = assignableByAllUsers };
        }
    }
}