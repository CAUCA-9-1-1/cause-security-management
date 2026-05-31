using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.ExternalSystem;

public class ExternalSystemLogonEndpoint
{
    [WolverinePost("/api/ExternalSystemAuthentication/Logon")]
    [AllowAnonymous]
    public static IResult Handle(ExternalSystemLoginInformations body, IExternalSystemAuthenticationService svc)
        => Logon(body, svc);

    [WolverinePost("/api/Authentication/LogonForExternalSystem")]
    [AllowAnonymous]
    public static IResult HandleLegacy(ExternalSystemLoginInformations body, IExternalSystemAuthenticationService svc)
        => Logon(body, svc);

    private static IResult Logon(ExternalSystemLoginInformations body, IExternalSystemAuthenticationService svc)
    {
        var (token, system) = svc.Login(body?.Apikey);
        if (system is null || token is null) return Results.Unauthorized();
        return Results.Ok(new LoginResult
        {
            AuthorizationType = SecurityManagementOptions.AuthenticationScheme,
            ExpiredOn = token.ExpiresOn,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            IdUser = system.Id,
            Name = system.Name,
        });
    }
}
