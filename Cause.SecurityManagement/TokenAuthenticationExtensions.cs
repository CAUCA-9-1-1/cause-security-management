using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

namespace Cause.SecurityManagement
{
    public static class TokenAuthenticationExtensions
    {
        public static IServiceCollection AddTokenAuthentication(this IServiceCollection services, SecurityConfiguration configuration)
        {
            var secretKey = configuration.SecretKey;
            var issuer = configuration.Issuer;
            var appName = configuration.PackageName;

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(config =>
            {
                config.RequireHttpsMetadata = false;
                config.SaveToken = true;
                config.TokenValidationParameters = GetAuthenticationParameters(secretKey, issuer, appName);
                config.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.StatusCode = 401;
                        if (context.Exception.GetType() == typeof(SecurityTokenException))
                            context.Response.Headers.Add("Token-Expired", "true");
                        else if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            context.Response.Headers.Add("Token-Expired", "true");
                        return Task.CompletedTask;
                    },
                };
            });
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });
            return services;
        }

        private static TokenValidationParameters GetAuthenticationParameters(string secretKey, string issuer, string appName)
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
}