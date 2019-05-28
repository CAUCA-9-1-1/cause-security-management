using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Antiforgery;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Cause.SecurityManagement.Antiforgery
{
    public class AuthorizeOrAntiforgeryAttribute : ActionFilterAttribute
    {
        public AuthorizeOrAntiforgeryAttribute()
        {
        }

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
                Console.WriteLine($"Is authorize : {authorize.ToString()}, Has Antiforgery : {antiforgery.ToString()}, Is DEV : {dev.ToString()}");
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
            var controller = filterContext.Controller as Controller;
            var userClaim = controller.User.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid);

            return (userClaim is null ? false : true);
        }

        private bool IsDev(ActionExecutingContext filterContext)
        {
            var env = filterContext.HttpContext.RequestServices.GetService<IHostingEnvironment>();

            return env.IsDevelopment();
        }
    }
}
