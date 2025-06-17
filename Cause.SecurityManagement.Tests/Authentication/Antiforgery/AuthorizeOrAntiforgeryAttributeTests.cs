using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Cause.SecurityManagement.Authentication.Antiforgery;
using AwesomeAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication.Antiforgery
{
    [AuthorizeOrAntiforgery]
    public class TestController : Controller;

	public class AuthorizeOrAntiforgeryAttributeTests
    {
        [Test]
        public void AsAnonymousUserWithToken_WhenAccessController_ShouldAccessController()
        {
            var context = GenerateBasicContext("", "", true);
            var controller = new AuthorizeOrAntiforgeryAttribute();

            controller.OnActionExecuting(context);

            context.Result.Should().Be(null);
        }

        [Test]
        public void AsAnonymousUserWithTokenOnMobile_WhenAccessController_ShouldAccessController()
        {
            var context = GenerateBasicContext("", "", false, true);
            var controller = new AuthorizeOrAntiforgeryAttribute();

            controller.OnActionExecuting(context);

            context.Result.Should().Be(null);
        }

        [Test]
        public void AsAuthorizeUser_WhenAccessController_ShouldAccessController()
        {
            var context = GenerateBasicContext("", "Bearer WithToken");
            var controller = new AuthorizeOrAntiforgeryAttribute();

            controller.OnActionExecuting(context);

            context.Result.Should().Be(null);
        }

        [Test]
        public void AsEnvironmentIsDev_WhenAccessController_ShouldAccessController()
        {
            var context = GenerateBasicContext("Development");
            var controller = new AuthorizeOrAntiforgeryAttribute();

            controller.OnActionExecuting(context);

            context.Result.Should().Be(null);
        }

        [Test]
        public void AsAnonymous_WhenAccessController_ShouldThrowsException()
        {
            var context = GenerateBasicContext();
            var controller = new AuthorizeOrAntiforgeryAttribute();
            
            controller.OnActionExecuting(context);

            context.Result.Should().NotBeNull().And.BeOfType<UnauthorizedResult>();
        }

        private ActionExecutingContext GenerateBasicContext(string environmentName = "", string headerAuthorization = "", bool validAntiforgery = false, bool validMobileAndToken = false)
        {
            Controller controller = null;
            var httpContext = GenerateBasicHttpContext(environmentName, validAntiforgery, validMobileAndToken);

            if (!string.IsNullOrEmpty(headerAuthorization))
            {
                controller = new TestController();
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = GetUser(headerAuthorization) }
                };
            }

            return new ActionExecutingContext(
                new ActionContext(
                    httpContext,
                    new RouteData(),
                    new ActionDescriptor(),
                    new ModelStateDictionary()
                ),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                controller!
            );
        }

        private static ClaimsPrincipal GetUser(string headerAuthorization)
        {
            var identity = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sid, headerAuthorization)
            ], "basic");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            return claimsPrincipal;
        }

        private static HttpContext GenerateBasicHttpContext(string environmentName = "", bool validAntiforgery = false, bool validMobileAndToken = false)
        {
            var antiforgery = Substitute.For<IAntiforgery>();

            if (validAntiforgery)
            {
                antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.FromResult(1));
            }
            else
            {
                antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.FromException(new AntiforgeryValidationException("")));
            }
            
            var webHostEnvironment = Substitute.For<IWebHostEnvironment>();
            webHostEnvironment.EnvironmentName = environmentName;

            var httpContext = Substitute.For<HttpContext>();
            httpContext.RequestServices.GetService<IAntiforgery>().Returns(antiforgery);
            httpContext.RequestServices.GetService<IWebHostEnvironment>().Returns(webHostEnvironment);

            if (validMobileAndToken)
            {
                httpContext.Request.Headers.UserAgent = "test mobile iPhone";
                httpContext.Request.Headers["X-CSRF-Cookie"] = "csrf cookie for test";
                httpContext.Request.Headers["X-CSRF-Token"] = "crsf cookie for test";
            }

            return httpContext;
        }
    }
}