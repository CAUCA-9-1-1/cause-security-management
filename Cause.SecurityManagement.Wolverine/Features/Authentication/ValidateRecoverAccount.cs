using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class ValidateRecoverAccountEndpoint
{
    [WolverinePost("/api/Authentication/RecoverAccountValidation")]
    [AllowAnonymous]
    public static async Task<IResult> HandleAsync(
        AccountRecoveryValidationRequest body,
        IEntityAuthenticator authenticator)
    {
        var result = await authenticator.ValidateAccountRecoveryAsync(body.Email, body.ValidationCode);
        return result is null ? Results.BadRequest() : Results.Ok(result);
    }
}
