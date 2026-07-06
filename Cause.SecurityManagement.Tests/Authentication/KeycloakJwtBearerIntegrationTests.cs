using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Authentication;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class KeycloakJwtBearerIntegrationTests
{
    private const string Issuer = "http://keycloak-test/realms/test";
    private const string Audience = "test-client";

    private RSA rsa;
    private RsaSecurityKey signingKey;
    private IHost metadataHost;
    private TestServer metadataServer;
    private IHost apiHost;
    private TestServer apiServer;

    [SetUp]
    public async Task SetUpTest()
    {
        rsa = RSA.Create(2048);
        signingKey = new RsaSecurityKey(rsa) { KeyId = "test-key" };

        metadataHost = await CreateMetadataHostAsync(signingKey, Issuer);
        metadataServer = metadataHost.GetTestServer();

        apiHost = await CreateApiHostAsync(metadataServer);
        apiServer = apiHost.GetTestServer();
    }

    [TearDown]
    public async Task TearDownTest()
    {
        if (apiHost != null)
            await apiHost.StopAsync();
        apiHost?.Dispose();

        if (metadataHost != null)
            await metadataHost.StopAsync();
        metadataHost?.Dispose();

        rsa?.Dispose();
    }

    [Test]
    public async Task ValidKeycloakToken_WhenCallingProtectedEndpoint_ShouldReturnOk()
    {
        var token = CreateSignedJwt(signingKey);
        using var client = apiServer.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/secure");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"authentication should succeed, but got: {body}");
    }

    private static async Task<IHost> CreateMetadataHostAsync(RsaSecurityKey key, string issuer)
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.Configure(app =>
                {
                    app.Map("/.well-known/openid-configuration", metadataApp => metadataApp.Run(async context =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            issuer,
                            jwks_uri = "http://keycloak-test/jwks"
                        }));
                    }));
                    app.Map("/jwks", jwksApp => jwksApp.Run(async context =>
                    {
                        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            keys = new[]
                            {
                                new { kty = jwk.Kty, use = "sig", kid = jwk.Kid, e = jwk.E, n = jwk.N, alg = SecurityAlgorithms.RsaSha256 }
                            }
                        }));
                    }));
                });
            });
        return await builder.StartAsync();
    }

    private static async Task<IHost> CreateApiHostAsync(TestServer metadataTestServer)
    {
        var securityConfiguration = new SecurityConfiguration
        {
            SecretKey = "test-secret-key-with-enough-length-1234567890",
            Issuer = "regular-user-issuer",
            PackageName = "regular-user-audience"
        };
        var keycloakConfiguration = new KeycloakConfiguration
        {
            MetadataAddress = "http://keycloak-test/.well-known/openid-configuration",
            ValidIssuer = Issuer,
            Resource = Audience,
            SslRequired = false,
            ShowDebugInfo = true
        };

        var builder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddTokenAuthentication(securityConfiguration, keycloakConfiguration);
                    services.Configure<JwtBearerOptions>(CustomAuthSchemes.KeycloakAuthentication, options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.BackchannelHttpHandler = metadataTestServer.CreateHandler();
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/secure", async context =>
                        {
                            var result = await context.AuthenticateAsync(CustomAuthSchemes.KeycloakAuthentication);
                            if (result.Succeeded)
                            {
                                context.Response.StatusCode = StatusCodes.Status200OK;
                                await context.Response.WriteAsync("ok");
                            }
                        });
                    });
                });
            });
        return await builder.StartAsync();
    }

    private static string CreateSignedJwt(RsaSecurityKey key)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
            new Claim("preferred_username", "test-user"),
            new Claim("role", "User")
        };
        var token = new JwtSecurityToken(Issuer, Audience, claims, notBefore: DateTime.UtcNow.AddMinutes(-1), expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: credentials);
        return handler.WriteToken(token);
    }
}
