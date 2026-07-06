using System;
using AwesomeAssertions;
using Cause.SecurityManagement.Authentication;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class KeycloakAuthenticationBuilderTests
{
    private JwtBearerOptions options;

    [SetUp]
    public void SetUpTest()
    {
        var securityConfiguration = new SecurityConfiguration
        {
            SecretKey = "test-secret-key-with-enough-length-1234567890",
            Issuer = "regular-user-issuer",
            PackageName = "regular-user-audience"
        };
        var keycloakConfiguration = new KeycloakConfiguration
        {
            MetadataAddress = "https://keycloak.example.com/.well-known/openid-configuration",
            ValidIssuer = "https://keycloak.example.com/realms/test",
            Resource = "test-client",
            SslRequired = true,
            ValidateSigningKey = true
        };

        var services = new ServiceCollection();
        services.AddTokenAuthentication(securityConfiguration, keycloakConfiguration);

        using var provider = services.BuildServiceProvider();
        options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(CustomAuthSchemes.KeycloakAuthentication);
    }

    [Test]
    public void KeycloakScheme_WhenConfigured_ShouldWireJwtBearerOptionsFromConfiguration()
    {
        options.Audience.Should().Be("test-client");
        options.MetadataAddress.Should().Be("https://keycloak.example.com/.well-known/openid-configuration");
        options.RequireHttpsMetadata.Should().BeTrue();
        options.SaveToken.Should().BeTrue();
    }

    [Test]
    public void KeycloakScheme_WhenConfigured_ShouldWireTokenValidationParametersFromConfiguration()
    {
        options.TokenValidationParameters.Should().NotBeNull();
        options.TokenValidationParameters.ValidIssuer.Should().Be("https://keycloak.example.com/realms/test");
        options.TokenValidationParameters.NameClaimType.Should().Be("preferred_username");
        options.TokenValidationParameters.RoleClaimType.Should().Be("role");
        options.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
        options.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        options.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        options.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        options.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
    }

    // Regression guard: the actual signing-key resolution bug (JwtBearer 10.0.9 failing every
    // Keycloak token with IDX10500 "No security keys were provided to validate the signature")
    // only reproduces through a real token-validation round-trip against an OIDC metadata
    // endpoint, not through inspecting configured values in isolation. That round-trip is
    // covered by KeycloakJwtBearerIntegrationTests, which fails if
    // KeycloakAuthenticationBuilder goes back to replacing options.TokenValidationParameters
    // wholesale instead of mutating the framework-provided instance in place.
}
