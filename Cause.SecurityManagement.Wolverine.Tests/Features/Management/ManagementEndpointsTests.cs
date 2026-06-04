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
    public async Task WhenGroupExists_DeleteGroup_ShouldReturnNoContent()
    {
        var groupId = Guid.NewGuid();
        groupService.DeleteGroupAsync(groupId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await DeleteGroupEndpoint.Handle(groupId, groupService, CancellationToken.None);

        StatusCodeOf(result).Should().Be(StatusCodes.Status204NoContent);
    }

    [Test]
    public async Task WhenGroupDoesNotExist_DeleteGroup_ShouldReturnNotFound()
    {
        groupService.DeleteGroupAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await DeleteGroupEndpoint.Handle(Guid.NewGuid(), groupService, CancellationToken.None);

        StatusCodeOf(result).Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task WhenGroupIsValid_SaveGroup_ShouldReturnOkWithSavedGroup()
    {
        var group = new GroupDto { Id = Guid.NewGuid(), Name = "Dispatchers" };
        var saved = new GroupDto { Id = group.Id, Name = "Dispatchers" };
        validator.ValidateAsync(Arg.Any<GroupDto>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        groupService.SaveGroupAsync(group, Arg.Any<CancellationToken>()).Returns(saved);

        var result = await SaveGroupEndpoint.Handle(group, groupService, validator, CancellationToken.None);

        result.Should().BeOfType<Ok<GroupDto>>().Which.Value.Should().Be(saved);
    }

    [Test]
    public async Task WhenGroupIsInvalid_SaveGroup_ShouldReturnBadRequest()
    {
        var group = new GroupDto { Id = Guid.NewGuid() };
        validator.ValidateAsync(Arg.Any<GroupDto>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));

        var result = await SaveGroupEndpoint.Handle(group, groupService, validator, CancellationToken.None);

        StatusCodeOf(result).Should().Be(StatusCodes.Status400BadRequest);
        await groupService.DidNotReceive().SaveGroupAsync(Arg.Any<GroupDto>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task WhenGroupExists_GetGroup_ShouldReturnOkWithGroup()
    {
        var groupId = Guid.NewGuid();
        var group = new GroupDto { Id = groupId, Name = "Dispatchers" };
        groupService.GetGroupAsync(groupId, Arg.Any<CancellationToken>()).Returns(group);

        var result = await GetGroupEndpoint.Handle(groupId, groupService, CancellationToken.None);

        result.Should().BeOfType<Ok<GroupDto>>().Which.Value.Should().Be(group);
    }

    [Test]
    public async Task WhenGroupDoesNotExist_GetGroup_ShouldReturnNotFound()
    {
        groupService.GetGroupAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GroupDto?)null);

        var result = await GetGroupEndpoint.Handle(Guid.NewGuid(), groupService, CancellationToken.None);

        StatusCodeOf(result).Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task WhenGroupHasMembers_GetUserList_ShouldReturnOkWithMembers()
    {
        var groupId = Guid.NewGuid();
        var members = new List<UserForGroupDto> { new() { Id = Guid.NewGuid(), FirstName = "Ada", LastName = "Lovelace" } };
        groupService.GetGroupUsersAsync(groupId, Arg.Any<CancellationToken>()).Returns(members);

        var result = await GetGroupUserListEndpoint.Handle(groupId, groupService, CancellationToken.None);

        result.Should().BeOfType<Ok<List<UserForGroupDto>>>().Which.Value.Should().BeEquivalentTo(members);
    }

    [Test]
    public async Task WhenSearching_SearchUsers_ShouldReturnOkWithResult()
    {
        var request = new UserSearchRequestDto { Query = "ada", Skip = 0, Top = 10 };
        var searchResult = new UserSearchResultDto { Items = new List<UserForGroupDto>(), TotalCount = 0 };
        groupService.SearchUsersAsync(request, Arg.Any<CancellationToken>()).Returns(searchResult);

        var result = await SearchUsersEndpoint.Handle(request, groupService, CancellationToken.None);

        result.Should().BeOfType<Ok<UserSearchResultDto>>().Which.Value.Should().Be(searchResult);
    }

    [Test]
    public async Task WhenCatalogRequested_GetPermissionCatalog_ShouldReturnOkWithCatalog()
    {
        var catalog = new List<PermissionDto>
        {
            new() { Id = Guid.NewGuid(), IdModulePermission = Guid.NewGuid(), Tag = "module.access", Name = "Access the module" }
        };
        permissionService.GetPermissionsAsync(Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await GetPermissionCatalogEndpoint.Handle(permissionService, CancellationToken.None);

        result.Should().BeOfType<Ok<List<PermissionDto>>>().Which.Value.Should().BeEquivalentTo(catalog);
    }

    private static int? StatusCodeOf(IResult result)
        => (result as IStatusCodeHttpResult)?.StatusCode;
}
