using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class SearchUsersEndpoint
{
    [WolverinePost("/GroupManagement/users/search")]
    public static async Task<IResult> Handle(
        UserSearchRequestDto request,
        IGroupManagementApiService groupService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await groupService.SearchUsersAsync(request, cancellationToken));
    }
}
