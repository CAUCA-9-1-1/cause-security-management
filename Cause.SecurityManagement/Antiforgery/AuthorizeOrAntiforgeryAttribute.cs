using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Antiforgery;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System;
using Microsoft.Extensions.Hosting;

namespace Cause.SecurityManagement.Antiforgery
{
    public class AuthorizeOrAntiforgeryAttribute : ActionFilterAttribute
    {
        public override async void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Console.WriteLine("Validation authorization");

            var authorize = IsAuthorize(filterContext);
            var antiforgery = await HasAntiforgery(filterContext);
            var dev = IsDev(filterContext);

            if (authorize || antiforgery || dev)
            {
                base.OnActionExecuting(filterContext);
            }
            else
            {
                Console.WriteLine($"Is authorize : {authorize}, Has Antiforgery : {antiforgery}, Is DEV : {dev}");
                filterContext.Result = new UnauthorizedResult();
            }
        }

        private async Task<bool> HasAntiforgery(ActionExecutingContext filterContext)
        {
            var antiforgery = filterContext.HttpContext.RequestServices.GetService<IAntiforgery>();

            try
            {
                await antiforgery.ValidateRequestAsync(filterContext.HttpContext);

                return true;
            }
            catch (AntiforgeryValidationException e)
            {
                Console.WriteLine($"Antiforgery is invalid : {e.Message}");
                return false;
            }
        }

        private bool IsAuthorize(ActionExecutingContext filterContext)
        {
            if (filterContext.Controller is Controller controller)
            {
                var userClaim =
                    controller.User.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid);
                return (!(userClaim is null));
            }

            return false;
        }

        private bool IsDev(ActionExecutingContext filterContext)
        {
            var env = filterContext.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
            return env.IsDevelopment();
        }
    }
}
