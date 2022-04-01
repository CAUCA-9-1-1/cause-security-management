using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
        private readonly IUserPermissionRepository permissionsReader;
        private readonly IAuthenticationService service;
        private readonly IExternalSystemAuthenticationService externalSystemAuthenticationService;
        private readonly IMobileVersionService mobileVersionService;
        private readonly ILogger<AuthenticationController> logger;

        public AuthenticationController(
            IUserPermissionRepository permissionsReader,
            IAuthenticationService service,
            IExternalSystemAuthenticationService externalSystemAuthenticationService,
            IMobileVersionService mobileVersionService,
            ILogger<AuthenticationController> logger)
        {
            this.permissionsReader = permissionsReader;
            this.service = service;
            this.externalSystemAuthenticationService = externalSystemAuthenticationService;
            this.mobileVersionService = mobileVersionService;
            this.logger = logger;
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        [ProducesResponseType(typeof(LoginResult), 200)]
        [ProducesResponseType(typeof(UnauthorizedResult), 401)]
        public async Task<ActionResult<LoginResult>> Logon([FromBody] LoginInformations login)
        {            
            var (token, user) = await service.LoginAsync(login?.UserName, login?.Password);
            if (user == null || token == null)
                return Unauthorized();

            return new LoginResult
            {
                AuthorizationType = "Bearer",
                ExpiredOn = token.ExpiresOn,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                MustChangePassword = user.PasswordMustBeResetAfterLogin,
                MustVerifyCode = service.MustValidateCode(user),
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
            catch (InvalidTokenException exception)
            {
                logger.LogWarning(exception, $"Could not refresh user's acess token.  Refresh token: '{tokens.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
                HttpContext.Response.Headers.Add("Token-Invalid", "true");
            }
            catch (SecurityTokenExpiredException exception)
            {
                HttpContext.Response.Headers.Add("Refresh-Token-Expired", "true");
                logger.LogWarning(exception, $"Could not refresh external system's acess token - SecurityTokenExpiredException.  Refresh token: '{tokens.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
            }
            catch (SecurityTokenException exception)
            {
                HttpContext.Response.Headers.Add("Token-Invalid", "true");
                logger.LogWarning(exception, $"Could not refresh external system's acess token - SecurityTokenException.  Refresh token: '{tokens.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
            }
            catch (InvalidTokenUserException exception)
            {
                HttpContext.Response.Headers.Add("Token-Invalid", "true");
                logger.LogWarning(exception, $"Could not refresh external system's acess token - InvalidTokenUserException.  Refresh token: '{tokens.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
            }

            return Unauthorized();
        }

        [Route("validationCode"), HttpGet, Authorize(Roles = SecurityRoles.UserLoginWithMultiFactor)]
        public async Task<ActionResult> SendNewCodeAsync()
        {
            try
            {
                await service.SendNewCodeAsync();
                return Ok();
            }
            catch (UserValidationCodeNotFoundException)
            {
                return Unauthorized();
            }
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
            catch (InvalidValidationCodeException exception)
            {
                return BadRequest(new { ErrorMessage = exception.Message });
            }
        }

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

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult RefreshForExternalSystem([FromBody] TokenRefreshResult tokens)
        {
            try
            {
                var newAccessToken = externalSystemAuthenticationService.RefreshAccessToken(tokens.AccessToken, tokens.RefreshToken);
                return Ok(new { AccessToken = newAccessToken, tokens.RefreshToken });
            }
            catch (InvalidTokenException exception)
            {
                logger.LogWarning(exception, $"Could not refresh external system's acess token.  Refresh token: '{tokens.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
                HttpContext.Response.Headers.Add("Token-Invalid", "true");
            }
            catch (SecurityTokenExpiredException)
            {
                HttpContext.Response.Headers.Add("Refresh-Token-Expired", "true");
            }
            catch (SecurityTokenException exception)
            {
                HttpContext.Response.Headers.Add("Token-Invalid", "true");
                logger.LogWarning(exception, $"Could not refresh external system's acess token - SecurityTokenException.  Refresh token: '{tokens.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
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