using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Authentication;

internal static class ServiceCollectionAuthenticationExtensions
{
    internal static AuthenticationBuilder AddAuthenticationWithScheme(this IServiceCollection services)
    {
        return services.AddAuthentication(options =>
        {
            options.DefaultScheme = CustomAuthSchemes.RegularUserOrKeycloakScheme;
            options.DefaultChallengeScheme = CustomAuthSchemes.RegularUserOrKeycloakScheme;
        });
    }

    internal static AuthenticationBuilder AddSimpleAuthentication(this IServiceCollection services)
    {
        return services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });
    }

    internal static IServiceCollection AddRedirectionCustomizationOnUnauthorized(this IServiceCollection services)
    {
        return services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
        });
    }
}