using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class GetGroupUserListEndpoint
{
    [WolverineGet("/GroupManagement/{groupId}/UserList")]
    public static IResult Handle(Guid groupId, IGroupManagementApiService groupService)
    {
        return Results.Ok(groupService.GetGroupUsers(groupId));
    }
}
