using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Authentication.Antiforgery
{
    public class AntiforgeryTokenProviderAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var antiforgery = filterContext.HttpContext.RequestServices.GetService<IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(filterContext.HttpContext);

            filterContext.HttpContext.Response.Headers.Add("X-CSRF-Token", tokens.RequestToken);
        }
    }
}
