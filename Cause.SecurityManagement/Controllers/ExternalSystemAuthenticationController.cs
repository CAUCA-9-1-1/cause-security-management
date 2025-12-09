using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers;

[Route("api/[controller]")]
public class ExternalSystemAuthenticationController(
    IExternalSystemAuthenticationService externalSystemAuthenticationService,
    ILogger<ExternalSystemAuthenticationController> logger) : ControllerBase
{
    [Route("[Action]"), Route("/api/Authentication/LogonForExternalSystem"), HttpPost, AllowAnonymous]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The login result with tokens", typeof(LoginResult))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
    [SwaggerOperation(Summary = "Authenticates an external system using an API key")]
    public ActionResult<LoginResult> Logon([FromBody] ExternalSystemLoginInformations login)
    {
        var (token, system) = externalSystemAuthenticationService.Login(login?.Apikey);
        if (system == null || token == null)
            return Unauthorized();

        return new LoginResult
        {
            AuthorizationType = SecurityManagementOptions.AuthenticationScheme,
            ExpiredOn = token.ExpiresOn,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            IdUser = system.Id,
            Name = system.Name,
        };
    }

    [Route("/api/Authentication/RefreshForExternalSystem"), Route("Refresh"), HttpPost, AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The new access token and refresh token")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
    [SwaggerOperation(Summary = "Refreshes the access token for an external system")]
    public async Task<ActionResult> RefreshForExternalSystemAsync([FromBody] TokenRefreshResult tokens)
    {
        try
        {
            var newAccessToken = await externalSystemAuthenticationService.RefreshAccessTokenAsync(tokens.AccessToken, tokens.RefreshToken);
            return Ok(new { AccessToken = newAccessToken, tokens.RefreshToken });
        }
        catch (InvalidTokenException exception)
        {
            logger.LogWarning(exception, $"Could not refresh external system's access token.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
            HttpContext.Response.Headers.Append("Token-Invalid", "true");
        }
        catch (SecurityTokenExpiredException)
        {
            HttpContext.Response.Headers.Append("Refresh-Token-Expired", "true");
        }
        catch (SecurityTokenException exception)
        {
            HttpContext.Response.Headers.Append("Token-Invalid", "true");
            logger.LogWarning(exception, $"Could not refresh external system's access token - SecurityTokenException.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
        }

        return Unauthorized();
    }
}