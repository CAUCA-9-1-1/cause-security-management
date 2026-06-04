using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Controllers.Management;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Controllers.Management
{
    [TestFixture]
    public class GroupManagementControllerTests
    {
        private IGroupManagementApiService groupService;
        private IValidator<GroupDto> validator;
        private TestableGroupManagementController controller;

        [SetUp]
        public void SetUp()
        {
            groupService = Substitute.For<IGroupManagementApiService>();
            validator = Substitute.For<IValidator<GroupDto>>();
            controller = new TestableGroupManagementController(groupService, validator)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                ProblemDetailsFactory = new TestProblemDetailsFactory(),
            };
        }

        [Test]
        public async Task WhenGroupExists_DeleteGroup_ShouldReturnNoContent()
        {
            var groupId = Guid.NewGuid();
            groupService.DeleteGroupAsync(groupId, Arg.Any<CancellationToken>()).Returns(true);

            var result = await controller.DeleteGroupAsync(groupId, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
        }

        [Test]
        public async Task WhenGroupDoesNotExist_DeleteGroup_ShouldReturnNotFound()
        {
            groupService.DeleteGroupAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

            var result = await controller.DeleteGroupAsync(Guid.NewGuid(), CancellationToken.None);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task WhenGroupIsValid_SaveGroup_ShouldReturnOkWithSavedGroup()
        {
            var group = new GroupDto { Id = Guid.NewGuid(), Name = "Dispatchers" };
            var saved = new GroupDto { Id = group.Id, Name = "Dispatchers" };
            validator.ValidateAsync(Arg.Any<GroupDto>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
            groupService.SaveGroupAsync(group, Arg.Any<CancellationToken>()).Returns(saved);

            var result = await controller.SaveGroupAsync(group, CancellationToken.None);

            (result.Result as OkObjectResult)?.Value.Should().Be(saved);
        }

        [Test]
        public async Task WhenGroupIsInvalid_SaveGroup_ShouldReturnBadRequest()
        {
            var group = new GroupDto { Id = Guid.NewGuid() };
            validator.ValidateAsync(Arg.Any<GroupDto>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));

            var result = await controller.SaveGroupAsync(group, CancellationToken.None);

            (result.Result as ObjectResult)?.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            await groupService.DidNotReceive().SaveGroupAsync(Arg.Any<GroupDto>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task WhenGroupExists_GetGroup_ShouldReturnOkWithGroup()
        {
            var groupId = Guid.NewGuid();
            var group = new GroupDto { Id = groupId, Name = "Dispatchers" };
            groupService.GetGroupAsync(groupId, Arg.Any<CancellationToken>()).Returns(group);

            var result = await controller.GetGroupAsync(groupId, CancellationToken.None);

            (result.Result as OkObjectResult)?.Value.Should().Be(group);
        }

        [Test]
        public async Task WhenGroupDoesNotExist_GetGroup_ShouldReturnNotFound()
        {
            groupService.GetGroupAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GroupDto)null);

            var result = await controller.GetGroupAsync(Guid.NewGuid(), CancellationToken.None);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        private sealed class TestableGroupManagementController(
            IGroupManagementApiService groupService,
            IValidator<GroupDto> groupValidator)
            : BaseGroupManagementController(groupService, groupValidator);

        private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
        {
            public override ProblemDetails CreateProblemDetails(
                HttpContext httpContext, int? statusCode = null, string title = null,
                string type = null, string detail = null, string instance = null)
                => new() { Status = statusCode };

            public override ValidationProblemDetails CreateValidationProblemDetails(
                HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null,
                string title = null, string type = null, string detail = null, string instance = null)
                => new(modelStateDictionary) { Status = statusCode ?? StatusCodes.Status400BadRequest };
        }
    }
}
