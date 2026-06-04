using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers.Management;

/// <summary>
/// Abstract, fully asynchronous endpoint exposing the catalog of assignable module permissions for
/// the group management UI. Subclass it in the host application to activate the endpoint, e.g.
/// <c>public class PermissionManagementController(IPermissionCatalogService service)
/// : BasePermissionManagementController(service);</c>.
/// </summary>
[Route("PermissionManagement")]
public abstract class BasePermissionManagementController(IPermissionCatalogService permissionService)
    : Controller
{
    protected IPermissionCatalogService PermissionService = permissionService;

    [HttpGet]
    [ProducesResponseType<List<PermissionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerResponse(StatusCodes.Status200OK, "The catalog of assignable module permissions", typeof(List<PermissionDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerOperation(
        Summary = "Retrieves the permission catalog",
        Description = "Returns every assignable module permission with its localization tag and description. Requires an authenticated user.")]
    public virtual async Task<ActionResult<List<PermissionDto>>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        return Ok(await PermissionService.GetPermissionsAsync(cancellationToken));
    }
}
