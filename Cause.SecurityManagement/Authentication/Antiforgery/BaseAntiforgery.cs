using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cause.SecurityManagement.Authentication.Antiforgery
{
    public class BaseAntiforgery : ActionFilterAttribute
    {
        protected static bool RequestIsFromMobile(HttpRequest request)
        {
            string userAgent = request.Headers.UserAgent.ToString() ?? "";

            return userAgent.Contains("iPhone") || userAgent.Contains("iPad") || userAgent.Contains("Android");
        }
    }
}
