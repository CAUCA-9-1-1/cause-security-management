using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class GetGroupEndpoint
{
    [WolverineGet("/GroupManagement/{groupId}")]
    public static async Task<IResult> Handle(
        Guid groupId,
        IGroupManagementApiService groupService,
        CancellationToken cancellationToken)
    {
        var group = await groupService.GetGroupAsync(groupId, cancellationToken);
        return group is null ? Results.NotFound() : Results.Ok(group);
    }
}
