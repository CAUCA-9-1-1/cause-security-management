using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cause.SecurityManagement.Controllers
{
	[Route("api/[controller]")]
	public class AuthentificationController : Controller
	{
		private readonly AuthentificationService service;
		private readonly string issuer;
		private readonly string applicationName;
		private readonly string secretKey;
		private readonly string minimalVersion;

		public AuthentificationController(AuthentificationService service, IConfiguration configuration)
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
			try
			{
				var newAccessToken = service.Refresh(tokens.AccessToken, tokens.RefreshToken, applicationName, issuer, secretKey);
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

		[HttpGet, Route("VersionValidator/{mobileVersion}"), AllowAnonymous]
		[ProducesResponseType(200)]
		[ProducesResponseType(401)]
		public ActionResult MobileVersionIsValid(string mobileVersion)
		{
			return Ok(service.IsMobileVersionValid(mobileVersion, minimalVersion));
		}
	}
}