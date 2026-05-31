using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class GetPermissionsEndpoint
{
    [WolverineGet("/api/Authentication/Permissions")]
    [AuthorizeForUserAndAdministratorRoles]
    public static async Task<IResult> HandleAsync(ICurrentUserService currentUserService)
    {
        return Results.Ok(await currentUserService.GetPermissionsAsync());
    }
}
