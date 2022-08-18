using System.Collections.ObjectModel;
using System.ComponentModel;
using NSubstitute.ReceivedExtensions;

namespace Cause.SecurityManagement.Tests.Services
{
    using Cause.SecurityManagement.Services;
    using TUser = Cause.SecurityManagement.Models.User;
    using System;
    using NUnit.Framework;
    using FluentAssertions;
    using NSubstitute;
    using Cause.SecurityManagement.Models;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Options;
    using Cause.SecurityManagement.Models.Configuration;
    using Cause.SecurityManagement.Repositories;

    [TestFixture]
    public class UserManagementServiceTests
    {
        private UserManagementService<TUser> userManagementService;
        private IOptions<SecurityConfiguration> configuration;
        private IUserGroupRepository userGroupRepository;
        private IUserPermissionRepository userPermissionRepository;
        private IGroupPermissionRepository groupPermissionRepository;
        private IUserRepository<TUser> userRepository;
        private IUserGroupPermissionService userGroupPermissionService;
        private IEmailForUserModificationSender emailSender;

        private readonly SecurityConfiguration securityConfiguration = new();

        private readonly UserGroup userGroupAAssignableByAllUsers = CreateUserGroup(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true);
        private readonly UserGroup userGroupBAssignableByAllUsers = CreateUserGroup(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab"), true);
        private readonly UserGroup userGroupNotAssignableByAllUsers = CreateUserGroup(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), false);


        private static Dictionary<UserGroup, bool> userGroupsAssignableByAllUsers = new();

        [SetUp]
        public void SetUp()
        {
            configuration = Options.Create(securityConfiguration);
            userGroupRepository = Substitute.For<IUserGroupRepository>();
            userPermissionRepository = Substitute.For<IUserPermissionRepository>();
            groupPermissionRepository = Substitute.For<IGroupPermissionRepository>();
            userRepository = Substitute.For<IUserRepository<TUser>>();
            userGroupPermissionService = Substitute.For<IUserGroupPermissionService>();
            emailSender = Substitute.For<IEmailForUserModificationSender>();
            userManagementService = new UserManagementService<TUser>(configuration, userGroupRepository, userPermissionRepository, groupPermissionRepository, userRepository, userGroupPermissionService, emailSender);
        }

        [Test]
        public void UserWithGroupsToRemove_WhenUpdateUserGroup_ShouldOnlyRemoveGroupAssignableByCurrentUser()
        {
            var userWithGroupToRemove = new User { Groups = new Collection<UserGroup>() };
            SetupRepository(new List<UserGroup> { userGroupAAssignableByAllUsers, userGroupBAssignableByAllUsers, userGroupNotAssignableByAllUsers });

            userManagementService.UpdateUserGroup(userWithGroupToRemove);

            userGroupRepository.Received(1).Remove(Arg.Is<UserGroup>(userGroupAAssignableByAllUsers));
            userGroupRepository.Received(1).Remove(Arg.Is<UserGroup>(userGroupBAssignableByAllUsers));
            userGroupRepository.DidNotReceive().Remove(Arg.Is<UserGroup>(userGroupNotAssignableByAllUsers));
        }


        private void SetupRepository(List<UserGroup> userGroups)
        {
            userGroups.ForEach(userGroup =>
            {
                userGroupPermissionService
                    .CurrentUserHasRequiredPermissionForGroupsAccess(Arg.Is<Guid>(userGroup.IdGroup))
                    .Returns(userGroupsAssignableByAllUsers[userGroup] );
            });
            userGroupRepository.GetForUser(Arg.Any<Guid>()).Returns(userGroups.AsQueryable());
        }

        private static UserGroup CreateUserGroup(Guid groupId, bool assignableByAllUsers)
        {
            var userGroup = new UserGroup { IdGroup = groupId  };
            userGroupsAssignableByAllUsers[userGroup] = assignableByAllUsers;
            return userGroup;
        }

    }
}