using AwesomeAssertions;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Cause.SecurityManagement.Wolverine.Features.Management;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Wolverine.Tests.Features.Management;

[TestFixture]
public class ManagementEndpointsTests
{
    private IGroupManagementApiService groupService = null!;
    private IPermissionCatalogService permissionService = null!;
    private IValidator<GroupDto> validator = null!;

    [SetUp]
    public void SetUp()
    {
        groupService = Substitute.For<IGroupManagementApiService>();
        permissionService = Substitute.For<IPermissionCatalogService>();
        validator = Substitute.For<IValidator<GroupDto>>();
    }

    [Test]
    public void WhenGroupExists_DeleteGroup_ShouldReturnNoContent()
    {
        var groupId = Guid.NewGuid();
        groupService.DeleteGroup(groupId).Returns(true);

        var result = DeleteGroupEndpoint.Handle(groupId, groupService);

        StatusCodeOf(result).Should().Be(StatusCodes.Status204NoContent);
    }

    [Test]
    public void WhenGroupDoesNotExist_DeleteGroup_ShouldReturnNotFound()
    {
        groupService.DeleteGroup(Arg.Any<Guid>()).Returns(false);

        var result = DeleteGroupEndpoint.Handle(Guid.NewGuid(), groupService);

        StatusCodeOf(result).Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public void WhenGroupIsValid_SaveGroup_ShouldReturnOkWithSavedGroup()
    {
        var group = new GroupDto { Id = Guid.NewGuid(), Name = "Dispatchers" };
        var saved = new GroupDto { Id = group.Id, Name = "Dispatchers" };
        validator.Validate(Arg.Any<GroupDto>()).Returns(new ValidationResult());
        groupService.SaveGroup(group).Returns(saved);

        var result = SaveGroupEndpoint.Handle(group, groupService, validator);

        result.Should().BeOfType<Ok<GroupDto>>().Which.Value.Should().Be(saved);
    }

    [Test]
    public void WhenGroupIsInvalid_SaveGroup_ShouldReturnBadRequest()
    {
        var group = new GroupDto { Id = Guid.NewGuid() };
        validator.Validate(Arg.Any<GroupDto>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));

        var result = SaveGroupEndpoint.Handle(group, groupService, validator);

        StatusCodeOf(result).Should().Be(StatusCodes.Status400BadRequest);
        groupService.DidNotReceive().SaveGroup(Arg.Any<GroupDto>());
    }

    [Test]
    public void WhenGroupExists_GetGroup_ShouldReturnOkWithGroup()
    {
        var groupId = Guid.NewGuid();
        var group = new GroupDto { Id = groupId, Name = "Dispatchers" };
        groupService.GetGroup(groupId).Returns(group);

        var result = GetGroupEndpoint.Handle(groupId, groupService);

        result.Should().BeOfType<Ok<GroupDto>>().Which.Value.Should().Be(group);
    }

    [Test]
    public void WhenGroupDoesNotExist_GetGroup_ShouldReturnNotFound()
    {
        groupService.GetGroup(Arg.Any<Guid>()).Returns((GroupDto?)null);

        var result = GetGroupEndpoint.Handle(Guid.NewGuid(), groupService);

        StatusCodeOf(result).Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public void WhenGroupHasMembers_GetUserList_ShouldReturnOkWithMembers()
    {
        var groupId = Guid.NewGuid();
        var members = new List<UserForGroupDto> { new() { Id = Guid.NewGuid(), FirstName = "Ada", LastName = "Lovelace" } };
        groupService.GetGroupUsers(groupId).Returns(members);

        var result = GetGroupUserListEndpoint.Handle(groupId, groupService);

        result.Should().BeOfType<Ok<List<UserForGroupDto>>>().Which.Value.Should().BeEquivalentTo(members);
    }

    [Test]
    public void WhenSearching_SearchUsers_ShouldReturnOkWithResult()
    {
        var request = new UserSearchRequestDto { Query = "ada", Skip = 0, Top = 10 };
        var searchResult = new UserSearchResultDto { Items = new List<UserForGroupDto>(), TotalCount = 0 };
        groupService.SearchUsers(request).Returns(searchResult);

        var result = SearchUsersEndpoint.Handle(request, groupService);

        result.Should().BeOfType<Ok<UserSearchResultDto>>().Which.Value.Should().Be(searchResult);
    }

    [Test]
    public void WhenCatalogRequested_GetPermissionCatalog_ShouldReturnOkWithCatalog()
    {
        var catalog = new List<PermissionDto>
        {
            new() { Id = Guid.NewGuid(), IdModulePermission = Guid.NewGuid(), Tag = "module.access", Name = "Access the module" }
        };
        permissionService.GetPermissions().Returns(catalog);

        var result = GetPermissionCatalogEndpoint.Handle(permissionService);

        result.Should().BeOfType<Ok<List<PermissionDto>>>().Which.Value.Should().BeEquivalentTo(catalog);
    }

    private static int? StatusCodeOf(IResult result)
        => (result as IStatusCodeHttpResult)?.StatusCode;
}
