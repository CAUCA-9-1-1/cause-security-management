using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cause.SecurityManagement.Authentication.Antiforgery
{
    public class AntiforgeryTokenProviderAttribute : BaseAntiforgery
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(context.HttpContext);

            if (!string.IsNullOrEmpty(tokens.RequestToken))
            {
                context.HttpContext.Response.Headers.Append("X-CSRF-Token", tokens.RequestToken);
            }

            if (RequestIsFromMobile(context.HttpContext.Request) && !string.IsNullOrEmpty(tokens.CookieToken))
            {
                context.HttpContext.Response.Headers.Append("X-CSRF-Cookie", tokens.CookieToken);
            }
        }
    }
}
