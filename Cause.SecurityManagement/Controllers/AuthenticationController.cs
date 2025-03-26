using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    [ProducesResponseType(200)]
    public ActionResult MobileVersionIsLatest(string mobileVersion)
    {
        return Ok(mobileVersionValidator.IsMobileVersionLatest(mobileVersion));
    }

    [HttpGet, Route("VersionValidator/{mobileVersion}"), AllowAnonymous]
    [ProducesResponseType(200)]
    public ActionResult MobileVersionIsValid(string mobileVersion)
    {
        return Ok(mobileVersionValidator.IsMobileVersionValid(mobileVersion));
    }

    [HttpGet, Route("Permissions")]
    [AuthorizeForUserAndAdministratorRoles]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> GetPermissionsAsync()
    {
        return Ok(await currentUserService.GetPermissionsAsync());
    }
}