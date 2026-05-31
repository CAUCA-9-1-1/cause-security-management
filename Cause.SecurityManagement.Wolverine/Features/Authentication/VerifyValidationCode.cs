using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class VerifyValidationCodeEndpoint
{
    [WolverinePost("/api/Authentication/ValidationCode")]
    [Authorize(Roles = SecurityRoles.UserLoginWithMultiFactor)]
    public static async Task<IResult> HandleAsync(
        ValidationInformation body,
        IEntityAuthenticator authenticator)
    {
        try
        {
            var result = await authenticator.ValidateMultiFactorCodeAsync(body);
            return Results.Ok(result);
        }
        catch (InvalidValidationCodeException exception)
        {
            return Results.BadRequest(new { exception.Message });
        }
    }
}
