using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class GetAuthenticationStateEndpoint
{
    [WolverinePost("/api/Authentication/State")]
    public static IResult Handle(
        AuthenticationStateRequest body,
        IEntityAuthenticator authenticator)
    {
        return Results.Ok(authenticator.IsLoggedIn(body.RefreshToken));
    }
}
