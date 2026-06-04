using System;
using System.Collections.Generic;
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
        public void WhenGroupExists_DeleteGroup_ShouldReturnNoContent()
        {
            var groupId = Guid.NewGuid();
            groupService.DeleteGroup(groupId).Returns(true);

            var result = controller.DeleteGroup(groupId);

            result.Should().BeOfType<NoContentResult>();
        }

        [Test]
        public void WhenGroupDoesNotExist_DeleteGroup_ShouldReturnNotFound()
        {
            groupService.DeleteGroup(Arg.Any<Guid>()).Returns(false);

            var result = controller.DeleteGroup(Guid.NewGuid());

            result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public void WhenGroupIsValid_SaveGroup_ShouldReturnOkWithSavedGroup()
        {
            var group = new GroupDto { Id = Guid.NewGuid(), Name = "Dispatchers" };
            var saved = new GroupDto { Id = group.Id, Name = "Dispatchers" };
            validator.Validate(Arg.Any<GroupDto>()).Returns(new ValidationResult());
            groupService.SaveGroup(group).Returns(saved);

            var result = controller.SaveGroup(group);

            (result.Result as OkObjectResult)?.Value.Should().Be(saved);
        }

        [Test]
        public void WhenGroupIsInvalid_SaveGroup_ShouldReturnBadRequest()
        {
            var group = new GroupDto { Id = Guid.NewGuid() };
            validator.Validate(Arg.Any<GroupDto>())
                .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));

            var result = controller.SaveGroup(group);

            (result.Result as ObjectResult)?.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            groupService.DidNotReceive().SaveGroup(Arg.Any<GroupDto>());
        }

        [Test]
        public void WhenGroupExists_GetGroup_ShouldReturnOkWithGroup()
        {
            var groupId = Guid.NewGuid();
            var group = new GroupDto { Id = groupId, Name = "Dispatchers" };
            groupService.GetGroup(groupId).Returns(group);

            var result = controller.GetGroup(groupId);

            (result.Result as OkObjectResult)?.Value.Should().Be(group);
        }

        [Test]
        public void WhenGroupDoesNotExist_GetGroup_ShouldReturnNotFound()
        {
            groupService.GetGroup(Arg.Any<Guid>()).Returns((GroupDto)null);

            var result = controller.GetGroup(Guid.NewGuid());

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public void WhenGroupHasMembers_GetUserList_ShouldReturnOkWithMembers()
        {
            var groupId = Guid.NewGuid();
            var members = new List<UserForGroupDto> { new() { Id = Guid.NewGuid(), FirstName = "Ada", LastName = "Lovelace" } };
            groupService.GetGroupUsers(groupId).Returns(members);

            var result = controller.GetUserList(groupId);

            (result.Result as OkObjectResult)?.Value.Should().BeEquivalentTo(members);
        }

        [Test]
        public void WhenSearching_SearchUsers_ShouldReturnOkWithResult()
        {
            var request = new UserSearchRequestDto { Query = "ada", Skip = 0, Top = 10 };
            var searchResult = new UserSearchResultDto { Items = new List<UserForGroupDto>(), TotalCount = 0 };
            groupService.SearchUsers(request).Returns(searchResult);

            var result = controller.SearchUsers(request);

            (result.Result as OkObjectResult)?.Value.Should().Be(searchResult);
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
