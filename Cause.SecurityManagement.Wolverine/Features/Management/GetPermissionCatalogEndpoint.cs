using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class GetPermissionCatalogEndpoint
{
    [WolverineGet("/PermissionManagement")]
    public static IResult Handle(IPermissionCatalogService permissionService)
    {
        return Results.Ok(permissionService.GetPermissions());
    }
}
