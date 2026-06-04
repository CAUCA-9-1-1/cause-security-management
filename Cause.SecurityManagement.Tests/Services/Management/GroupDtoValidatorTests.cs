using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services.Management
{
    [TestFixture]
    public class GroupDtoValidatorTests
    {
        private GroupDtoValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new GroupDtoValidator();
        }

        [Test]
        public void WithValidGroup_ShouldBeValid()
        {
            var group = CreateValidGroup();

            var result = validator.Validate(group);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void WithEmptyId_ShouldBeInvalid()
        {
            var group = CreateValidGroup();
            group.Id = Guid.Empty;

            var result = validator.Validate(group);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void WithEmptyName_ShouldBeInvalid()
        {
            var group = CreateValidGroup();
            group.Name = "";

            var result = validator.Validate(group);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void WithNameLongerThanHundredCharacters_ShouldBeInvalid()
        {
            var group = CreateValidGroup();
            group.Name = new string('a', 101);

            var result = validator.Validate(group);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void WithPermissionMissingModulePermission_ShouldBeInvalid()
        {
            var group = CreateValidGroup();
            group.Permissions = new List<GroupPermissionDto>
            {
                new() { Id = Guid.NewGuid(), IdGroup = group.Id, IdModulePermission = Guid.Empty, IsAllowed = true }
            };

            var result = validator.Validate(group);

            result.IsValid.Should().BeFalse();
        }

        private static GroupDto CreateValidGroup()
        {
            return new GroupDto
            {
                Id = Guid.NewGuid(),
                Name = "Dispatchers",
                AssignableByAllUsers = true,
                Permissions = new List<GroupPermissionDto>(),
                Users = new List<GroupUserDto>(),
            };
        }
    }
}
