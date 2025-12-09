using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Controllers;

[Route("api/[controller]")]
public class AuthenticationController(
    ICurrentUserService currentUserService,
    IUserAuthenticator userAuthenticator,
    IUserTokenRefresher userTokenRefresher,
    IMobileVersionValidator mobileVersionValidator,
    ILogger<AuthenticationController> logger)
    : BaseAuthenticationController (userAuthenticator, userTokenRefresher, logger)
{
    [HttpGet, Route("VersionValidator/{mobileVersion}/Latest"), AllowAnonymous]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Indicates whether the mobile version is the latest available version", typeof(bool))]
    [SwaggerOperation(Summary = "Checks if the provided mobile version is the latest available version")]
    public ActionResult MobileVersionIsLatest(string mobileVersion)
    {
        return Ok(mobileVersionValidator.IsMobileVersionLatest(mobileVersion));
    }

    [HttpGet, Route("VersionValidator/{mobileVersion}"), AllowAnonymous]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "Indicates whether the mobile version is valid and supported", typeof(bool))]
    [SwaggerOperation(Summary = "Checks if the provided mobile version is valid and supported")]
    public ActionResult MobileVersionIsValid(string mobileVersion)
    {
        return Ok(mobileVersionValidator.IsMobileVersionValid(mobileVersion));
    }

    [HttpGet, Route("Permissions")]
    [AuthorizeForUserAndAdministratorRoles]
    [ProducesResponseType<List<AuthenticationUserPermission>>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The list of permissions for the user", typeof(List<AuthenticationUserPermission>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
    [SwaggerOperation(
        Summary = "Retrieves the list of permissions for the currently authenticated user",
        Description = "Requires one of the following roles: RegularUser, Administrator")]
    public async Task<ActionResult> GetPermissionsAsync()
    {
        return Ok(await currentUserService.GetPermissionsAsync());
    }
}