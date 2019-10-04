using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cause.SecurityManagement.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : AuthentifiedController
    {
        private readonly IAuthentificationService service;
        private readonly string issuer;
        private readonly string applicationName;
        private readonly string secretKey;
        private readonly string minimalVersion;

        public AuthenticationController(IAuthentificationService service, IConfiguration configuration)
        {
            this.service = service;
            issuer = configuration.GetSection("APIConfig:Issuer").Value;
            applicationName = configuration.GetSection("APIConfig:PackageName").Value;
            secretKey = configuration.GetSection("APIConfig:SecretKey").Value;
            minimalVersion = configuration.GetSection("APIConfig:MinimalVersion").Value;
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult<LoginResult> Logon([FromBody] LoginInformations login)
        {
            return Login(login);
        }

        [Route("/api/authentification/login"), HttpPost, AllowAnonymous]
        public ActionResult<LoginResult> OldLogon([FromBody] LoginInformations login)
        {
            return Login(login);
        }

        private ActionResult<LoginResult> Login(LoginInformations login)
        {
            var result = service.Login(login.UserName, login.Password, applicationName, issuer, secretKey);
            if (result.user == null || result.token == null)
                return Unauthorized();

            return new LoginResult
            {
                AuthorizationType = "Bearer",
                ExpiredOn = result.token.ExpiresOn,
                AccessToken = result.token.AccessToken,
                RefreshToken = result.token.RefreshToken,
                IdUser = result.user.Id,
                Name = result.user.FirstName + " " + result.user.LastName,
            };
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult Refresh([FromBody] TokenRefreshResult tokens)
        {
            return RefreshToken(tokens);
        }

        [Route("api/authentification/refresh"), HttpPost, AllowAnonymous]
        public ActionResult OldRefresh([FromBody] TokenRefreshResult tokens)
        {
            return RefreshToken(tokens);
        }

        private ActionResult RefreshToken(TokenRefreshResult tokens)
        {
            try
            {
                var newAccessToken = service.RefreshUserToken(tokens.AccessToken, tokens.RefreshToken, applicationName, issuer,
                    secretKey);
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
        public ActionResult<LoginResult> LogonForExternalSystemn([FromBody] ExternalSystemLoginInformations login)
        {
            var result = service.LoginForExternalSystem(login.Apikey, applicationName, issuer, secretKey);
            if (result.system == null || result.token == null)
                return Unauthorized();

            return new LoginResult
            {
                AuthorizationType = "ApiBearer",
                ExpiredOn = result.token.ExpiresOn,
                AccessToken = result.token.AccessToken,
                RefreshToken = result.token.RefreshToken,
                IdUser = result.system.Id,
                Name = result.system.Name,
            };
        }

        [Route("[Action]"), HttpPost, AllowAnonymous]
        public ActionResult RefreshForExternalSystem([FromBody] TokenRefreshResult tokens)
        {
            try
            {
                var newAccessToken = service.RefreshUserToken(tokens.AccessToken, tokens.RefreshToken, applicationName, issuer,
                    secretKey);
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

        [HttpGet, Route("VersionValidator/{mobileVersion}"), AllowAnonymous]
        [ProducesResponseType(200)]
        public ActionResult MobileVersionIsValid(string mobileVersion)
        {
            return Ok(service.IsMobileVersionValid(mobileVersion, minimalVersion));
        }

        [HttpGet, Route("Permissions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public ActionResult GetPermissions()
        {
            return Ok(service.GetActiveUserPermissions(GetUserId()));
        }
    }
}