using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class DeleteGroupEndpoint
{
    [WolverineDelete("/GroupManagement/{groupId}")]
    public static IResult Handle(Guid groupId, IGroupManagementApiService groupService)
    {
        return groupService.DeleteGroup(groupId) ? Results.NoContent() : Results.NotFound();
    }
}
