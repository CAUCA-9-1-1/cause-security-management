using System;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        options.Events = new() { OnAuthenticationFailed = context => GetCustomOnAuthenticationFailedResult(context, configuration) };
    }

    private static async Task GetCustomOnAuthenticationFailedResult(AuthenticationFailedContext context, KeycloakConfiguration configuration)
    {
        if (context.Exception is SecurityTokenException && context.HttpContext.User.Identity?.IsAuthenticated == false)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.AddOrUpdate("Token-Expired", "true");

            if (configuration.ShowDebugInfo)
            {
                context.Response.ContentType = "application/json";
                
                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var signingKeys = context.Options.TokenValidationParameters.IssuerSigningKeys?
                    .Select(k => new { k.KeyId, Type = k.GetType().Name }).ToArray()
                    ?? Array.Empty<object>();

                var signingKey = context.Options.TokenValidationParameters.IssuerSigningKey;

                var errorResponse = new
                {
                    Error = "Authentication failed",
                    ExceptionType = context.Exception.GetType().Name,
                    context.Exception.Message,
                    CurrentConfiguration = new
                    {
                        configuration.Audience,
                        configuration.ValidIssuer,
                        configuration.MetadataAddress,
                        configuration.SslRequired,
                        configuration.ValidateSigningKey
                    },
                    JwtBearerOptions = new
                    {
                        context.Options.Authority,
                        context.Options.MetadataAddress,
                        context.Options.Audience,
                        HasSigningKey = signingKey != null,
                        SigningKeyId = signingKey?.KeyId,
                        SigningKeysCount = signingKeys.Length,
                        SigningKeys = signingKeys
                    },
                    Token = token,
                    User = new
                    {
                        IsAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated ?? false,
                        context.HttpContext.User.Identity?.AuthenticationType,
                        context.HttpContext.User.Identity?.Name,
                        Claims = context.HttpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
                    }
                };
                
                var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(json));
            }
        }
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