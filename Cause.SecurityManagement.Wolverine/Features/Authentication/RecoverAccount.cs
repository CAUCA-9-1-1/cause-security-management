using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class RecoverAccountEndpoint
{
    [WolverinePost("/api/Authentication/RecoverAccount")]
    [AllowAnonymous]
    public static async Task<IResult> HandleAsync(
        AccountRecoveryRequest body,
        IEntityAuthenticator authenticator)
    {
        await authenticator.RecoverAccountAsync(body.Email);
        return Results.NoContent();
    }
}
