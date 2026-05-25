using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cause.SecurityManagement.Authentication.Antiforgery
{
    public class BaseAntiforgery : ActionFilterAttribute
    {
        protected static bool RequestIsFromMobile(HttpRequest request)
        {
            string userAgent = request.Headers.UserAgent.ToString() ?? "";
            string platform = request.Headers["Sec-CH-UA-Platform"].ToString().Trim('"');

            return userAgent.Contains("iPhone")
                || userAgent.Contains("iPad")
                || userAgent.Contains("Android")
                || platform == "iOS"
                || platform == "Android";
        }
    }
}
