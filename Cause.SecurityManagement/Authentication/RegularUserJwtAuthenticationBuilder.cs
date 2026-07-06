using Cause.SecurityManagement.Core.Authentication;
using System.Text;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Cause.SecurityManagement.Authentication;

internal static class RegularUserJwtAuthenticationBuilder
{
    internal static AuthenticationBuilder AddRegularUserJwtBearer(this AuthenticationBuilder builder, SecurityConfiguration configuration)
    {
        return builder.AddJwtBearer(CustomAuthSchemes.RegularUserAuthentication, 
            config => GetDefaultJwtBearerOptions(config, configuration.SecretKey, configuration.Issuer, configuration.PackageName));
    }

    internal static AuthenticationBuilder AddSimpleAuthentication(this AuthenticationBuilder builder, SecurityConfiguration configuration)
    {
        return builder.AddJwtBearer(config => GetDefaultJwtBearerOptions(config, configuration.SecretKey, configuration.Issuer, configuration.PackageName));
    }

    private static void GetDefaultJwtBearerOptions(JwtBearerOptions options, string secretKey, string issuer, string appName)
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = GetAuthenticationParameters(secretKey, issuer, appName);
        options.Events = new JwtBearerEvents { OnAuthenticationFailed = GetCustomOnAuthenticationFailedResult };
    }

    internal static Task GetCustomOnAuthenticationFailedResult(AuthenticationFailedContext context)
    {
        if (context.Exception is SecurityTokenException && context.HttpContext.User.Identity?.IsAuthenticated == false)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.AddOrUpdate("Token-Expired", "true");
        }
        return Task.CompletedTask;
    }

    internal static TokenValidationParameters GetAuthenticationParameters(string secretKey, string issuer, string appName)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = appName,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        return tokenValidationParameters;
    }
}