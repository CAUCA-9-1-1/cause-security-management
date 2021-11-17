using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
        private readonly IUserPermissionReader permissionsReader;
        private readonly IAuthenticationService service;
        private readonly IExternalSystemAuthenticationService externalSystemAuthenticationService;
        private readonly IMobileVersionService mobileVersionService;

        public AuthenticationController(
            IUserPermissionReader permissionsReader,
            IAuthenticationService service,
            IExternalSystemAuthenticationService externalSystemAuthenticationService,
            IMobileVersionService mobileVersionService)
        {
            this.permissionsReader = permissionsReader;
            this.service = service;
            this.externalSystemAuthenticationService = externalSystemAuthenticationService;
            this.mobileVersionService = mobileVersionService;
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public async Task<ActionResult<LoginResult>> Logon([FromBody] LoginInformations login)
        {            
            var (token, user) = await service.LoginAsync(login.UserName, login.Password);
            if (user == null || token == null)
                return Unauthorized();

            return new LoginResult
            {
                AuthorizationType = "Bearer",
                ExpiredOn = token.ExpiresOn,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                MustChangePassword = user.PasswordMustBeResetAfterLogin,
                MustVerifyCode = SecurityManagementOptions.MultiFactorAuthenticationIsActivated,
                IdUser = user.Id,
                Name = user.FirstName + " " + user.LastName,
            };
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult Refresh([FromBody] TokenRefreshResult tokens)
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

        [Route("validationCode"), HttpPost, Authorize(Roles = SecurityRoles.UserLoginWithMultiFactor)]
        public ActionResult<LoginResult> VerifyCode([FromBody] ValidationInformation validationInformation)
        {
            try
            {
                var (token, user) = service.ValidateMultiFactorCode(validationInformation);
                return new LoginResult
                {
                    AuthorizationType = "Bearer",
                    ExpiredOn = token.ExpiresOn,
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken,
                    MustChangePassword = user.PasswordMustBeResetAfterLogin,
                    MustVerifyCode = false,
                    IdUser = user.Id,
                    Name = user.FirstName + " " + user.LastName,
                };
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
        public ActionResult GetPermissions()
        {
            return Ok(permissionsReader.GetActiveUserPermissions());
        }
    }
}