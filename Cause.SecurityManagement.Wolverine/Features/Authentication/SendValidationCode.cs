using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class SendValidationCodeEndpoint
{
    [WolverineGet("/api/Authentication/validationCode")]
    [Authorize(Roles = SecurityRoles.UserLoginWithMultiFactor)]
    public static async Task<IResult> HandleAsync(
        ValidationCodeCommunicationType communicationType,
        IEntityAuthenticator authenticator)
    {
        try
        {
            await authenticator.SendNewCodeAsync(communicationType);
            return Results.Ok();
        }
        catch (UserValidationCodeNotFoundException)
        {
            return Results.Unauthorized();
        }
    }
}
