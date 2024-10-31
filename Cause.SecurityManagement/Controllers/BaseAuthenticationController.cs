using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models.ValidationCode;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers;

public abstract class BaseAuthenticationController(
    IEntityAuthenticator authenticator, 
    IEntityTokenRefresher tokenRefresher,
    ILogger<AuthenticationController> logger) : Controller
{
    protected readonly ILogger<AuthenticationController> Logger = logger;

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
        var result = await authenticator.LoginAsync(login?.UserName, login?.Password);
        return result == null ? Unauthorized() : result;
    }

    [Route("validationCode"), HttpGet, Authorize(Roles = SecurityRoles.UserLoginWithMultiFactor)]
    public async Task<ActionResult> SendNewCodeAsync([FromQuery]ValidationCodeCommunicationType communicationType = ValidationCodeCommunicationType.Sms)
    {
        try
        {
            await authenticator.SendNewCodeAsync(communicationType);
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
            return await authenticator.ValidateMultiFactorCodeAsync(validationInformation);
        }
        catch (InvalidValidationCodeException exception)
        {
            return BadRequest(new { ErrorMessage = exception.Message });
        }
    }

    [HttpPost, Route("State")]
    [ProducesResponseType(200)]
    public bool GetAuthenticationState([FromBody] AuthenticationStateRequest requestBody)
    {
        return authenticator.IsLoggedIn(requestBody.RefreshToken);
    }

    [Route("Refresh"), HttpPost, AllowAnonymous]
    public async Task<ActionResult> GetNewAccessTokenAsync([FromBody] TokenRefreshResult tokens)
    {
        try
        {
            var newAccessToken = await tokenRefresher.GetNewAccessTokenAsync(tokens.AccessToken, tokens.RefreshToken);
            return Ok(new { AccessToken = newAccessToken, tokens.RefreshToken });
        }
        catch (InvalidTokenException exception)
        {
            Logger.LogWarning(exception, "Could not refresh user's acess token.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'", tokens?.RefreshToken, tokens?.AccessToken);
            HttpContext.Response.Headers.Append("Token-Invalid", "true");
        }
        catch (SecurityTokenExpiredException exception)
        {
            HttpContext.Response.Headers.Append("Refresh-Token-Expired", "true");
            Logger.LogWarning(exception, $"Could not refresh external system's acess token - SecurityTokenExpiredException.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
        }

        catch (SecurityTokenException exception)
        {
            HttpContext.Response.Headers.Append("Token-Invalid", "true");
            Logger.LogWarning(exception, $"Could not refresh external system's acess token - SecurityTokenException.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
        }
        catch (InvalidTokenUserException exception)
        {
            HttpContext.Response.Headers.Append("Token-Invalid", "true");
            Logger.LogWarning(exception, $"Could not refresh external system's acess token - InvalidTokenUserException.  Refresh token: '{tokens?.RefreshToken}'.  Access token: '{tokens?.AccessToken}'");
        }

        return Unauthorized();
    }

    [HttpPost, Route("RecoverAccount"), AllowAnonymous]
    [SwaggerOperation(Summary = "Recover account")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Recovery email has perharps been sent")]
    public async Task<ActionResult> RecoverAccountAsync([FromBody] AccountRecoveryRequest request)
    {
        await authenticator.RecoverAccountAsync(request.Email);
        return NoContent();
    }

    [HttpPost, Route("RecoverAccountValidation"), AllowAnonymous]
    [SwaggerOperation(Summary = "Validate account recovery")]
    [SwaggerResponse(StatusCodes.Status200OK, "Account validated", type: typeof(LoginResult))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid code")]
    public async Task<ActionResult> ValidateRecoverAccount([FromBody] AccountRecoveryValidationRequest request)
    {
        var result = await authenticator.ValidateAccountRecoveryAsync(request.Email, request.ValidationCode);
        return result == null ? BadRequest() : Ok(result);
    }

    [HttpPost, Route("PasswordSetup"), AuthorizeByRoles(SecurityRoles.User, SecurityRoles.UserPasswordSetup, SecurityRoles.UserRecovery)]
    [SwaggerOperation(
        Summary = "Set password for user",
        Description = "Requires an anthenticated user, user in password setup or user in recovery")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Password has been set")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
    public ActionResult UpdatePassword([FromBody] PasswordChangeRequest request)
    {
        authenticator.ChangePassword(request.NewPassword);
        return NoContent();
    }
}