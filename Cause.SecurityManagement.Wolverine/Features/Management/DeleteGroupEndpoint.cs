using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class DeleteGroupEndpoint
{
    [WolverineDelete("/GroupManagement/{groupId}")]
    public static async Task<IResult> Handle(
        Guid groupId,
        IGroupManagementApiService groupService,
        CancellationToken cancellationToken)
    {
        return await groupService.DeleteGroupAsync(groupId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }
}
