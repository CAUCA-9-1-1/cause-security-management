using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers.Management;

/// <summary>
/// Abstract, fully asynchronous endpoint for the server-side paged active-user search used when
/// picking group members. Kept separate from group management so each controller has a single
/// responsibility. Subclass it in the host application to activate the endpoint, e.g.
/// <c>public class UserSearchController(IUserSearchService service) : BaseUserSearchController(service);</c>.
/// </summary>
[Route("UserSearch")]
public abstract class BaseUserSearchController(IUserSearchService userSearchService)
    : Controller
{
    protected IUserSearchService UserSearchService = userSearchService;

    [HttpPost]
    [ProducesResponseType<UserSearchResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerResponse(StatusCodes.Status200OK, "A page of matching active users with the total match count", typeof(UserSearchResultDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "The caller is not authenticated")]
    [SwaggerOperation(
        Summary = "Searches active users",
        Description = "Performs a server-side paged search over all active users, matching the query against first or last name and excluding already-selected users. Requires an authenticated user.")]
    public virtual async Task<ActionResult<UserSearchResultDto>> SearchUsersAsync([FromBody] UserSearchRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await UserSearchService.SearchUsersAsync(request, cancellationToken));
    }
}
