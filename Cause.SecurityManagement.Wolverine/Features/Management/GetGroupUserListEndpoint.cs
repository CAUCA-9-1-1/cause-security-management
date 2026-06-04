using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class GetGroupUserListEndpoint
{
    [WolverineGet("/GroupManagement/{groupId}/UserList")]
    public static async Task<IResult> Handle(
        Guid groupId,
        IGroupManagementApiService groupService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await groupService.GetGroupUsersAsync(groupId, cancellationToken));
    }
}
