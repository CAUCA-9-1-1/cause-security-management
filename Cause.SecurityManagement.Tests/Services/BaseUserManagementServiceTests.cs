using System.Collections.ObjectModel;
using Cause.SecurityManagement.Services;
using TUser = Cause.SecurityManagement.Models.User;
using System;
using NUnit.Framework;
using NSubstitute;
using Cause.SecurityManagement.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using AwesomeAssertions;

namespace Cause.SecurityManagement.Tests.Services
{
    [TestFixture]
    public class UserManagementServiceTests
    {
        private UserManagementService<TUser> userManagementService;
        private IOptions<SecurityConfiguration> configuration;
        private IUserGroupRepository userGroupRepository;
        private IUserPermissionRepository userPermissionRepository;
        private IUserRepository<TUser> userRepository;
        private IUserGroupPermissionService userGroupPermissionService;
        private IUserPermissionService userPermissionService;
        private IEmailForUserModificationSender emailSender;

        private readonly SecurityConfiguration securityConfiguration = new();

        private readonly UserGroup userGroupAAssignableByAllUsers = CreateUserGroup(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true);
        private readonly UserGroup userGroupBAssignableByAllUsers = CreateUserGroup(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab"), true);
        private readonly UserGroup userGroupNotAssignableByAllUsers = CreateUserGroup(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), false);
        private static readonly Dictionary<UserGroup, bool> UserGroupsAssignableByAllUsers = new();

        [SetUp]
        public void SetUp()
        {
            securityConfiguration.PackageName = "Cause.SecurityManagement";
            configuration = Options.Create(securityConfiguration);
            userGroupRepository = Substitute.For<IUserGroupRepository>();
            userPermissionRepository = Substitute.For<IUserPermissionRepository>();
            userRepository = Substitute.For<IUserRepository<TUser>>();
            userGroupPermissionService = Substitute.For<IUserGroupPermissionService>();
            userPermissionService = Substitute.For<IUserPermissionService>();
            emailSender = Substitute.For<IEmailForUserModificationSender>();
            userManagementService = new UserManagementService<TUser>(configuration, userGroupRepository, userPermissionRepository, userRepository, userGroupPermissionService, userPermissionService, emailSender);
        }

        [Test]
        public void UserWithGroupsToRemove_WhenUpdateUserGroup_ShouldOnlyRemoveGroupAssignableByCurrentUser()
        {
            var userWithGroupToRemove = new User { Groups = new Collection<UserGroup>() };
            SetupRepository(new List<UserGroup> { userGroupAAssignableByAllUsers, userGroupBAssignableByAllUsers, userGroupNotAssignableByAllUsers });

            userManagementService.UpdateUserGroup(userWithGroupToRemove);

            userGroupRepository.Received(1).Remove(Arg.Is(userGroupAAssignableByAllUsers));
            userGroupRepository.Received(1).Remove(Arg.Is(userGroupBAssignableByAllUsers));
            userGroupRepository.DidNotReceive().Remove(Arg.Is(userGroupNotAssignableByAllUsers));
        }

        [Test]
        public void ValidateCurrentPasswordActivated_WhenChangingPassword_ShouldReturnFalseWhenCurrentPasswordIsNotTheSame()
        {
            SecurityManagementOptions.ValidateCurrentPasswordOnPasswordChange = true;
            var currentPassword = "currentPassword";
            var encodedPassword = new PasswordGenerator().EncodePassword(currentPassword, securityConfiguration.PackageName);
            var user = new User { Password = encodedPassword };
            userRepository.Get(Arg.Is(user.Id)).Returns(user);
            var newPassword = "newPassword";

            var result= userManagementService.ChangePassword(user.Id, newPassword, false, "wrongPassword");

            result.Should().BeFalse();
            userRepository.DidNotReceive().SaveChanges();
        }
        
        [Test]
        public void ValidateCurrentPasswordActivated_WhenChangingPassword_ShouldSaveNewPasswordWhenCurrentPasswordIsTheSame()
        {
            SecurityManagementOptions.ValidateCurrentPasswordOnPasswordChange = true;
            var currentPassword = "currentPassword";
            var encodedPassword = new PasswordGenerator().EncodePassword(currentPassword, securityConfiguration.PackageName);
            var user = new User { Password = encodedPassword };
            userRepository.Get(Arg.Is(user.Id)).Returns(user);
            var newPassword = "newPassword";

            var result= userManagementService.ChangePassword(user.Id, newPassword, false, currentPassword);

            result.Should().BeTrue();
            user.Password.Should().Be(new PasswordGenerator().EncodePassword(newPassword, securityConfiguration.PackageName)); 
            userRepository.Received(1).SaveChanges();
        }
        
        [Test]
        public void ValidateCurrentPasswordNotActivated_WhenChangingPassword_ShouldSaveUserWithNewPassword()
        {
            SecurityManagementOptions.ValidateCurrentPasswordOnPasswordChange = false;
            var user = new User { Password = "" };
            var newPassword = "newPassword";
            userRepository.Get(Arg.Is(user.Id)).Returns(user);

            var result= userManagementService.ChangePassword(user.Id, newPassword, false);

            result.Should().BeTrue();
            user.Password.Should().Be(new PasswordGenerator().EncodePassword(newPassword, securityConfiguration.PackageName));
            userRepository.Received(1).SaveChanges();
        }

        private void SetupRepository(List<UserGroup> userGroups)
        {
            userGroups.ForEach(userGroup =>
            {
                userGroupPermissionService
                    .CurrentUserHasRequiredPermissionForGroupsAccess(Arg.Is(userGroup.IdGroup))
                    .Returns(UserGroupsAssignableByAllUsers[userGroup] );
            });
            userGroupRepository.GetForUser(Arg.Any<Guid>()).Returns(userGroups);
        }

        private static UserGroup CreateUserGroup(Guid groupId, bool assignableByAllUsers)
        {
            var userGroup = new UserGroup { IdGroup = groupId  };
            UserGroupsAssignableByAllUsers[userGroup] = assignableByAllUsers;
            return userGroup;
        }

    }
}