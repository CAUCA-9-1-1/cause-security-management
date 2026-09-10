using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Controllers.Management;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Controllers.Management
{
    [TestFixture]
    public class UserPermissionDetailsControllerTests
    {
        private IUserPermissionDetailReader reader;
        private Guid someUserId;

        [SetUp]
        public void SetUp()
        {
            reader = Substitute.For<IUserPermissionDetailReader>();
            someUserId = Guid.NewGuid();
            reader.GetForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new List<UserPermissionDetailDto>());
        }

        [Test]
        public async Task CallerAllowedToSeeTheUser_WhenGetForUser_ShouldReturnTheDetails()
        {
            var details = new List<UserPermissionDetailDto>
            {
                new() { Tag = "CanEditBuilding", Name = "Edit a building", Status = PermissionStatus.Allowed }
            };
            reader.GetForUserAsync(someUserId, Arg.Any<CancellationToken>()).Returns(details);
            var controller = new TestableController(reader, canView: true);

            var result = await controller.GetForUserAsync(someUserId, CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeSameAs(details);
        }

        [Test]
        public async Task CallerNotAllowedToSeeTheUser_WhenGetForUser_ShouldReturnForbidden()
        {
            var controller = new TestableController(reader, canView: false);

            var result = await controller.GetForUserAsync(someUserId, CancellationToken.None);

            result.Result.Should().BeOfType<StatusCodeResult>()
                .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Test]
        public async Task CallerNotAllowedToSeeTheUser_WhenGetForUser_ShouldNotReadAnyPermission()
        {
            var controller = new TestableController(reader, canView: false);

            await controller.GetForUserAsync(someUserId, CancellationToken.None);

            await reader.DidNotReceive().GetForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        private sealed class TestableController(IUserPermissionDetailReader reader, bool canView)
            : BaseUserPermissionDetailsController(reader)
        {
            protected override Task<bool> CanViewPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
                => Task.FromResult(canView);
        }
    }
}
