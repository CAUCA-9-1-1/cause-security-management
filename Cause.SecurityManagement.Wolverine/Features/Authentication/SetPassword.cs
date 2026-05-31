using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class SetPasswordEndpoint
{
    [WolverinePost("/api/Authentication/PasswordSetup")]
    [AuthorizeByRoles(SecurityRoles.User, SecurityRoles.UserPasswordSetup, SecurityRoles.UserRecovery)]
    public static IResult Handle(
        PasswordChangeRequest body,
        IEntityAuthenticator authenticator)
    {
        authenticator.ChangePassword(body.NewPassword);
        return Results.NoContent();
    }
}
