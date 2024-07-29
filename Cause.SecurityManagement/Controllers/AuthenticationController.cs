using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Cause.SecurityManagement.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController(
        ICurrentUserService currentUserService,
        IUserAuthenticator userAuthenticator,
        IUserTokenRefresher userTokenRefresher,
        IExternalSystemAuthenticationService externalSystemAuthenticationService,
        IMobileVersionService mobileVersionService,
        ILogger<AuthenticationController> logger)
        : BaseAuthenticationController (userAuthenticator, userTokenRefresher, logger)
    {
        [Route("[Action]"), HttpPost, AllowAnonymous]
        [ProducesResponseType(typeof(LoginResult), 200)]
        [ProducesResponseType(typeof(UnauthorizedResult), 401)]
        public ActionResult<LoginResult> LogonForExternalSystem([FromBody] ExternalSystemLoginInformations login)
        {
            var (token, system) = externalSystemAuthenticationService.Login(login?.Apikey);
            if (system == null || token == null)
                return Unauthorized();

            return new LoginResult
            {
                AuthorizationType = "Bearer",
                ExpiredOn = token.ExpiresOn,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                IdUser = system.Id,
                Name = system.Name,
            };
        }

        [Route("RefreshForExternalSystem"), HttpPost, AllowAnonymous]
        public async Task<ActionResult> RefreshForExternalSystemAsync([FromBody] TokenRefreshResult tokens)
        {
            try
            {
                var newAccessToken = await externalSystemAuthenticationService.RefreshAccessTokenAsync(tokens.AccessToken, tokens.RefreshToken);
                return Ok(new { AccessToken = newAccessToken, tokens.RefreshToken });
            }
            catch (InvalidTokenException exception)
            {
                Logger.LogWarning(exception, $"Could not refresh external system's access token.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
                HttpContext.Response.Headers.Append("Token-Invalid", "true");
            }
            catch (SecurityTokenExpiredException)
            {
                HttpContext.Response.Headers.Append("Refresh-Token-Expired", "true");
            }
            catch (SecurityTokenException exception)
            {
                HttpContext.Response.Headers.Append("Token-Invalid", "true");
                Logger.LogWarning(exception, $"Could not refresh external system's access token - SecurityTokenException.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
            }

            return Unauthorized();
        }

        [HttpGet, Route("VersionValidator/{mobileVersion}/Latest"), AllowAnonymous]
        [ProducesResponseType(200)]
        public ActionResult MobileVersionIsLatest(string mobileVersion)
        {
            return Ok(mobileVersionService.IsMobileVersionLatest(mobileVersion));
        }

        [HttpGet, Route("VersionValidator/{mobileVersion}"), AllowAnonymous]
        [ProducesResponseType(200)]
        public ActionResult MobileVersionIsValid(string mobileVersion)
        {
            return Ok(mobileVersionService.IsMobileVersionValid(mobileVersion));
        }

        [HttpGet, Route("Permissions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetPermissionsAsync()
        {
            return Ok(await currentUserService.GetPermissionsAsync());
        }
    }
}
