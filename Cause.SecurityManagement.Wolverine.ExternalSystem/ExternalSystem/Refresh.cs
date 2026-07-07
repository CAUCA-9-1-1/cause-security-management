using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.ExternalSystem;

public class ExternalSystemRefreshEndpoint
{
    [WolverinePost("/api/ExternalSystemAuthentication/Refresh")]
    [AllowAnonymous]
    public static Task<IResult> HandleAsync(TokenRefreshResult body, HttpContext httpContext, IExternalSystemAuthenticationService svc, ILogger<ExternalSystemRefreshEndpoint> logger)
        => RefreshAsync(body, httpContext, svc, logger);

    [WolverinePost("/api/Authentication/RefreshForExternalSystem")]
    [AllowAnonymous]
    public static Task<IResult> HandleLegacyAsync(TokenRefreshResult body, HttpContext httpContext, IExternalSystemAuthenticationService svc, ILogger<ExternalSystemRefreshEndpoint> logger)
        => RefreshAsync(body, httpContext, svc, logger);

    private static async Task<IResult> RefreshAsync(TokenRefreshResult body, HttpContext httpContext, IExternalSystemAuthenticationService svc, ILogger<ExternalSystemRefreshEndpoint> logger)
    {
        try
        {
            var newAccessToken = await svc.RefreshAccessTokenAsync(body.AccessToken, body.RefreshToken);
            return Results.Ok(new { AccessToken = newAccessToken, body.RefreshToken });
        }
        catch (InvalidTokenException exception)
        {
            logger.LogWarning(exception, "Could not refresh external system's access token. Refresh token: '{RefreshToken}'. Access token: '{AccessToken}'", body.RefreshToken, body.AccessToken);
            httpContext.Response.Headers.Append("Token-Invalid", "true");
        }
        catch (SecurityTokenExpiredException)
        {
            httpContext.Response.Headers.Append("Refresh-Token-Expired", "true");
        }
        catch (SecurityTokenException exception)
        {
            logger.LogWarning(exception, "Could not refresh external system's access token - SecurityTokenException. Refresh token: '{RefreshToken}'. Access token: '{AccessToken}'", body.RefreshToken, body.AccessToken);
            httpContext.Response.Headers.Append("Token-Invalid", "true");
        }

        return Results.Unauthorized();
    }
}
