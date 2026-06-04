using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class GetGroupEndpoint
{
    [WolverineGet("/GroupManagement/{groupId}")]
    public static IResult Handle(Guid groupId, IGroupManagementApiService groupService)
    {
        var group = groupService.GetGroup(groupId);
        return group is null ? Results.NotFound() : Results.Ok(group);
    }
}
