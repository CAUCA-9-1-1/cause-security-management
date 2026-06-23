using System;
using System.Linq.Expressions;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services.Management
{
    [TestFixture]
    public class UserProjectionExtensionsTests
    {
        private static Expression<Func<User, GroupUserDto>> BaseProjection =>
            user => new GroupUserDto { Id = user.Id, FullName = user.FirstName + " " + user.LastName };

        [Test]
        public void ValidBaseProjection_WithAdditionalInformation_ShouldPopulateAllMembers()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };

            var projected = BaseProjection.WithAdditionalInformation(u => u.Email).Compile()(user);

            projected.Id.Should().Be(userId);
            projected.FullName.Should().Be("Alice Smith");
            projected.AdditionalInformation.Should().Be("alice@example.com");
        }

        [Test]
        public void ValidBaseProjection_WithAdditionalInformationReturningNull_ShouldLeaveAdditionalInformationNull()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, FirstName = "Bob", LastName = "Jones", Email = "bob@example.com" };

            var projected = BaseProjection.WithAdditionalInformation(u => null).Compile()(user);

            projected.Id.Should().Be(userId);
            projected.FullName.Should().Be("Bob Jones");
            projected.AdditionalInformation.Should().BeNull();
        }

        [Test]
        public void NonMemberInitBaseProjection_WithAdditionalInformation_ShouldThrowArgumentException()
        {
            var captured = new GroupUserDto { Id = Guid.NewGuid(), FullName = "captured" };
            Expression<Func<User, GroupUserDto>> nonInit = user => captured;

            var act = () => nonInit.WithAdditionalInformation(u => u.Email);

            act.Should().Throw<ArgumentException>()
                .WithParameterName("baseProjection");
        }
    }
}
