using Cause.SecurityManagement.Core.Authentication;
using System.Text;
using System.Text.Json;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Cause.SecurityManagement.Authentication;

internal static class KeycloakAuthenticationBuilder
{
    internal static AuthenticationBuilder AddKeycloakAuthenticationBuilderWhenNeeded(this AuthenticationBuilder builder, KeycloakConfiguration? configuration)
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
        ConfigureValidationParameters(options.TokenValidationParameters, configuration);
        options.Events = new()
        {
            OnAuthenticationFailed = context => GetCustomOnAuthenticationFailedResult(context, configuration)
        };
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
                var discoveredConfiguration = await GetDiscoveredConfigurationForDiagnosticsAsync(context);

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
                    DiscoveredConfiguration = discoveredConfiguration,
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

    private static async Task<object?> GetDiscoveredConfigurationForDiagnosticsAsync(AuthenticationFailedContext context)
    {
        if (context.Options.ConfigurationManager == null)
            return null;

        try
        {
            var discoveredConfiguration = await context.Options.ConfigurationManager.GetConfigurationAsync(context.HttpContext.RequestAborted);
            return new
            {
                discoveredConfiguration.Issuer,
                SigningKeysCount = discoveredConfiguration.SigningKeys?.Count ?? 0,
                SigningKeyIds = discoveredConfiguration.SigningKeys?.Select(key => key.KeyId).ToArray() ?? Array.Empty<string>()
            };
        }
        catch (Exception exception)
        {
            return new { Error = exception.Message };
        }
    }

    private static void ConfigureValidationParameters(TokenValidationParameters validationParameters, KeycloakConfiguration configuration)
    {
        validationParameters.ValidIssuer = configuration.ValidIssuer;
        validationParameters.ValidateIssuer = true;
        validationParameters.ValidateAudience = true;
        validationParameters.ValidateIssuerSigningKey = configuration.ValidateSigningKey;
        validationParameters.NameClaimType = "preferred_username";
        validationParameters.RoleClaimType = "role";
        validationParameters.ValidateLifetime = true;
        validationParameters.ClockSkew = TimeSpan.Zero;
    }
}