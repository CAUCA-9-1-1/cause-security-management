using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Cause.SecurityManagement.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
        private readonly IAuthenticationService service;
        private readonly IExternalSystemAuthenticationService externalSystemAuthenticationService;

        public AuthenticationController(
            IAuthenticationService service,
            IExternalSystemAuthenticationService externalSystemAuthenticationService)
        {
            this.service = service;
            this.externalSystemAuthenticationService = externalSystemAuthenticationService;
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult<LoginResult> Logon([FromBody] LoginInformations login)
        {
            return Login(login);
        }

        private ActionResult<LoginResult> Login(LoginInformations login)
        {
            var (token, user) = service.Login(login.UserName, login.Password);
            if (user == null || token == null)
                return Unauthorized();

            return new LoginResult
            {
                AuthorizationType = "Bearer",
                ExpiredOn = token.ExpiresOn,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                IdUser = user.Id,
                Name = user.FirstName + " " + user.LastName,
            };
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult Refresh([FromBody] TokenRefreshResult tokens)
        {
            return RefreshToken(tokens);
        }

        private ActionResult RefreshToken(TokenRefreshResult tokens)
        {
            try
            {
                var newAccessToken = service.RefreshUserToken(tokens.AccessToken, tokens.RefreshToken);
                return Ok(new {AccessToken = newAccessToken, tokens.RefreshToken});
            }
            catch (SecurityTokenExpiredException)
            {
                HttpContext.Response.Headers.Add("Refresh-Token-Expired", "true");
            }
            catch (SecurityTokenException)
            {
                HttpContext.Response.Headers.Add("Token-Invalid", "true");
            }

            return Unauthorized();
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult<LoginResult> LogonForExternalSystem([FromBody] ExternalSystemLoginInformations login)
        {
            var (token, system) = externalSystemAuthenticationService.LoginForExternalSystem(login.Apikey);
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

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult RefreshForExternalSystem([FromBody] TokenRefreshResult tokens)
        {
            try
            {
                var newAccessToken = externalSystemAuthenticationService.RefreshExternalSystemToken(tokens.AccessToken, tokens.RefreshToken);
                return Ok(new { AccessToken = newAccessToken, tokens.RefreshToken });
            }
            catch (SecurityTokenExpiredException)
            {
                HttpContext.Response.Headers.Add("Refresh-Token-Expired", "true");
            }
            catch (SecurityTokenException)
            {
                HttpContext.Response.Headers.Add("Token-Invalid", "true");
            }

            return Unauthorized();
        }

        [HttpGet, Route("VersionValidator/{mobileVersion}/Latest"), AllowAnonymous]
        [ProducesResponseType(200)]
        public ActionResult MobileVersionIsLatest(string mobileVersion)
        {
            return Ok(service.IsMobileVersionLatest(mobileVersion));
        }

        [HttpGet, Route("VersionValidator/{mobileVersion}"), AllowAnonymous]
        [ProducesResponseType(200)]
        public ActionResult MobileVersionIsValid(string mobileVersion)
        {
            return Ok(service.IsMobileVersionValid(mobileVersion));
        }

        [HttpGet, Route("Permissions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public ActionResult GetPermissions()
        {
            return Ok(service.GetActiveUserPermissions());
        }
    }
}