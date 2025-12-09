using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Cause.SecurityManagement.Authentication;

public class MultipleSchemeRequireUserAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var authorize = UserIsAuthenticated(filterContext.HttpContext.User);

        if (authorize)
        {
            base.OnActionExecuting(filterContext);
        }
        else
        {
            filterContext.Result = new UnauthorizedResult();
        }
    }

    private static bool UserIsAuthenticated(ClaimsPrincipal claims)
    {
        var isTokenAuthenticated = claims.HasClaim(claim => claim.Type == JwtRegisteredClaimNames.Sid);
        var isCertificateAuthenticated = claims.HasClaim(claim => claim.Type == ClaimTypes.Sid);

        return isTokenAuthenticated || isCertificateAuthenticated;
    }
}