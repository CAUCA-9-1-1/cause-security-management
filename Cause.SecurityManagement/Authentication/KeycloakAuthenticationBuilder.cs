using System;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Cause.SecurityManagement.Authentication;

internal static class KeycloakAuthenticationBuilder
{
    internal static AuthenticationBuilder AddKeycloakAuthenticationBuilderWhenNeeded(this AuthenticationBuilder builder, KeycloakConfiguration configuration)
    {
        return configuration != null ? builder.AddKeycloakJwtBearer(configuration) : builder;
    }

    private static AuthenticationBuilder AddKeycloakJwtBearer(this AuthenticationBuilder builder, KeycloakConfiguration configuration)
    {
        return builder
            .AddJwtBearer(CustomAuthSchemes.KeycloakAuthentication, config => GetDefautKeycloakBearerOptions(config, configuration));
    }

    private static void GetDefautKeycloakBearerOptions(JwtBearerOptions options, KeycloakConfiguration configuration)
    {
        options.RequireHttpsMetadata = configuration.SslRequired;
        options.Audience = configuration.Audience;
        options.MetadataAddress = configuration.MetadataAddress;
        options.SaveToken = true;
        options.TokenValidationParameters = GetValidationParameters(configuration);
        options.Events = new() { OnAuthenticationFailed = GetCustomOnAuthenticationFailedResult };
    }

    private static Task GetCustomOnAuthenticationFailedResult(AuthenticationFailedContext context)
    {
        if (context.Exception is SecurityTokenException && context.HttpContext.User.Identity?.IsAuthenticated == false)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.AddOrUpdate("Token-Expired", "true");
        }
        return Task.CompletedTask;
    }

    private static TokenValidationParameters GetValidationParameters(KeycloakConfiguration configuration)
    {
        return new()
        {
            ValidIssuer = configuration.ValidIssuer,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = configuration.ValidateSigningKey,
            NameClaimType = "preferred_username",
            RoleClaimType = "role",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}