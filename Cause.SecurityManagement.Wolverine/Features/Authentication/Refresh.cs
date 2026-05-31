using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class RefreshEndpoint
{
    [WolverinePost("/api/Authentication/Refresh")]
    [AllowAnonymous]
    public static async Task<IResult> HandleAsync(
        TokenRefreshResult body,
        HttpContext httpContext,
        IEntityTokenRefresher tokenRefresher,
        ILogger<RefreshEndpoint> logger)
    {
        try
        {
            var newAccessToken = await tokenRefresher.GetNewAccessTokenAsync(body.AccessToken, body.RefreshToken);
            return Results.Ok(new { AccessToken = newAccessToken, body.RefreshToken });
        }
        catch (InvalidTokenException exception)
        {
            logger.LogWarning(exception, "Could not refresh user's access token. Refresh token: '{RefreshToken}'. Access token: '{AccessToken}'", body.RefreshToken, body.AccessToken);
            httpContext.Response.Headers.Append("Token-Invalid", "true");
        }
        catch (SecurityTokenExpiredException exception)
        {
            logger.LogWarning(exception, "Could not refresh user's access token - SecurityTokenExpiredException. Refresh token: '{RefreshToken}'. Access token: '{AccessToken}'", body.RefreshToken, body.AccessToken);
            httpContext.Response.Headers.Append("Refresh-Token-Expired", "true");
        }
        catch (SecurityTokenException exception)
        {
            logger.LogWarning(exception, "Could not refresh user's access token - SecurityTokenException. Refresh token: '{RefreshToken}'. Access token: '{AccessToken}'", body.RefreshToken, body.AccessToken);
            httpContext.Response.Headers.Append("Token-Invalid", "true");
        }
        catch (InvalidTokenUserException exception)
        {
            logger.LogWarning(exception, "Could not refresh user's access token - InvalidTokenUserException. Refresh token: '{RefreshToken}'. Access token: '{AccessToken}'", body.RefreshToken, body.AccessToken);
            httpContext.Response.Headers.Append("Token-Invalid", "true");
        }

        return Results.Unauthorized();
    }
}
