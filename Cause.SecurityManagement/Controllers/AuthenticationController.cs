using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Cause.SecurityManagement.Authentication.MultiFactor;
using Microsoft.AspNetCore.Http;

namespace Cause.SecurityManagement.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController(
        ICurrentUserService currentUserService,
        IAuthenticationService service,
        IExternalSystemAuthenticationService externalSystemAuthenticationService,
        IMobileVersionService mobileVersionService,
        ILogger<AuthenticationController> logger)
        : Controller
    {
        [Route("[Action]"), HttpPost, AllowAnonymous]
        [ProducesResponseType(typeof(LoginResult), 200)]
        [ProducesResponseType(typeof(UnauthorizedResult), 401)]
        public async Task<ActionResult<LoginResult>> Logon([FromHeader(Name = "auth")] string authorizationHeader, [FromBody] LoginInformations loginInformations)
        {
            var login = GetLoginFromHeader(authorizationHeader) ?? loginInformations;
            if (login == null)
                return Unauthorized();
            return await Logon(login);
        }

        private static LoginInformations GetLoginFromHeader(string authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader))
                return null;
            var decodedHeader = Uri.UnescapeDataString(Encoding.Default.GetString(Convert.FromBase64String(authorizationHeader)));
            var login = JsonSerializer.Deserialize<LoginInformations>(decodedHeader, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return login;
        }

        private async Task<ActionResult<LoginResult>> Logon(LoginInformations login)
        {
            var (token, user) = await service.LoginAsync(login?.UserName, login?.Password);
            if (user == null || token == null)
                return Unauthorized();

            return new LoginResult
            {
                AuthorizationType = "Bearer",
                AccessToken = token.AccessToken,
                ExpiredOn = token.ExpiresOn,
                RefreshToken = token.RefreshToken,
                MustChangePassword = user.PasswordMustBeResetAfterLogin,
                MustVerifyCode = service.MustValidateCode(user),
                IdUser = user.Id,
                Name = user.FirstName + " " + user.LastName,
            };
        }

        [Route("Refresh"), HttpPost, AllowAnonymous]
        public async Task<ActionResult> RefreshAsync([FromBody] TokenRefreshResult tokens)
        {
            try
            {
                var newAccessToken = await service.RefreshUserTokenAsync(tokens.AccessToken, tokens.RefreshToken);
                return Ok(new {AccessToken = newAccessToken, tokens.RefreshToken});
            }
            catch (InvalidTokenException exception)
            {
                logger.LogWarning(exception, "Could not refresh user's acess token.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'", tokens?.RefreshToken, tokens?.AccessToken);
                HttpContext.Response.Headers.Append("Token-Invalid", "true");
            }
            catch (SecurityTokenExpiredException exception)
            {
                HttpContext.Response.Headers.Append("Refresh-Token-Expired", "true");
                logger.LogWarning(exception, $"Could not refresh external system's acess token - SecurityTokenExpiredException.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
            }
            catch (SecurityTokenException exception)
            {
                HttpContext.Response.Headers.Append("Token-Invalid", "true");
                logger.LogWarning(exception, $"Could not refresh external system's acess token - SecurityTokenException.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
            }
            catch (InvalidTokenUserException exception)
            {
                HttpContext.Response.Headers.Append("Token-Invalid", "true");
                logger.LogWarning(exception, $"Could not refresh external system's acess token - InvalidTokenUserException.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
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

        [Route("ValidationCode"), HttpPost, Authorize(Roles = SecurityRoles.UserLoginWithMultiFactor)]
        public async Task<ActionResult<LoginResult>> VerifyCode([FromBody] ValidationInformation validationInformation)
        {
            try
            {
                var (token, user) = await service.ValidateMultiFactorCodeAsync(validationInformation);
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