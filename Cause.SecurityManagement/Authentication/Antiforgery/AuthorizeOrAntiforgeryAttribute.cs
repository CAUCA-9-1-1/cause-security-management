using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Cause.SecurityManagement.Authentication.Antiforgery;

[AttributeUsage(AttributeTargets.Method|AttributeTargets.Class)]
public class AuthorizeOrAntiforgeryAttribute : BaseAntiforgery
{
    public override async void OnActionExecuting(ActionExecutingContext context)
    {
        Console.WriteLine("Validation authorization");

        var authorize = await IsAuthorize(context);
        var antiforgery = await HasAntiforgery(context);
        var isFromMobile = RequestIsFromMobile(context.HttpContext.Request);
        var dev = IsDev(context);

        if (authorize || antiforgery || isFromMobile || dev)
        {
            base.OnActionExecuting(context);
        }
        else
        {
            Console.WriteLine($"Is authorize : {authorize}, Has Antiforgery : {antiforgery}, Is From Mobile : {isFromMobile}, Is DEV : {dev}");
            context.Result = new UnauthorizedResult();
        }
    }

    private static async Task<bool> HasAntiforgery(ActionExecutingContext filterContext)
    {
        var antiforgery = filterContext.HttpContext.RequestServices.GetService<IAntiforgery>();

        try
        {
            await antiforgery!.ValidateRequestAsync(filterContext.HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException e)
        {
            Console.WriteLine($"Antiforgery is invalid : {e.Message}");
            return false;
        }
    }

    private static async Task<bool> IsAuthorize(ActionExecutingContext filterContext)
    {
        var authResult = await filterContext.HttpContext.AuthenticateAsync();
        if (!authResult.Succeeded)
            return false;

        var user = authResult.Principal;
        return user.HasClaim(c => c.Type == JwtRegisteredClaimNames.Sid)
               || user.HasClaim(c => c.Type == ClaimTypes.Sid);
    }

    private static bool IsDev(ActionExecutingContext filterContext)
    {
        var env = filterContext.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
        return env!.IsDevelopment();
    }
}