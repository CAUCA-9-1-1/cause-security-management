using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class GetPermissionCatalogEndpoint
{
    [WolverineGet("/PermissionManagement")]
    public static async Task<IResult> Handle(
        IPermissionCatalogService permissionService,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await permissionService.GetPermissionsAsync(cancellationToken));
    }
}
