using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class SearchUsersEndpoint
{
    [WolverinePost("/UserSearch")]
    public static async Task<IResult> Handle(
        UserSearchRequestDto request,
        IUserSearchService userSearchService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await userSearchService.SearchUsersAsync(request, cancellationToken));
    }
}
