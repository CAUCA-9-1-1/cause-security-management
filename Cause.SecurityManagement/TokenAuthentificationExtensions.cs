using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cause.SecurityManagement
{
    public static class TokenAuthentificationExtensions
    {
        public static IServiceCollection AddTokenAuthentification(this IServiceCollection services, IConfiguration configuration)
        {
            var secretKey = configuration.GetSection("APIConfig:SecretKey").Value;
            var issuer = configuration.GetSection("APIConfig:Issuer").Value;
            var appName = configuration.GetSection("APIConfig:PackageName").Value;

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
                    OnTokenValidated = OnValidateTokenInDatabase
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

        private static Task OnValidateTokenInDatabase(TokenValidatedContext context)
        {
	        if (Guid.TryParse(context.Principal.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value, out Guid userId))
	        {
		        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthentificationService>();
                Console.WriteLine("authService.SetCurrentUser");
		        authService.SetCurrentUser(userId);
	        }

	        return Task.CompletedTask;
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