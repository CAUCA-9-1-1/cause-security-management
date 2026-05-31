using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Me;

public class GetCurrentUserEndpoint
{
    [WolverineGet("/api/Me")]
    [AuthorizeForUserAdministratorAndCertificateRoles]
    public static IResult Handle(HttpContext httpContext)
    {
        return Results.Ok(httpContext.User.Claims
            .Select(c => new { c.Type, c.Value })
            .ToList());
    }
}
