using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Authentication.Antiforgery
{
    public class AntiforgeryTokenProviderAttribute : BaseAntiforgery
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var antiforgery = context.HttpContext.RequestServices.GetService<IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(context.HttpContext);

            context.HttpContext.Response.Headers.Append("X-CSRF-Token", tokens.RequestToken);

            if (RequestIsFromMobile(context.HttpContext.Request))
            {
                context.HttpContext.Response.Headers.Append("X-CSRF-Cookie", tokens.CookieToken);
            }
        }
    }
}
