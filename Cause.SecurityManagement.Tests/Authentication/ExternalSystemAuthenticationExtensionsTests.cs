using System;
using AwesomeAssertions;
using Cause.SecurityManagement.Authentication;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class ExternalSystemAuthenticationExtensionsTests
{
    private JwtBearerOptions tokenOptions;
    private PolicySchemeOptions policyOptions;

    [SetUp]
    public void SetUpTest()
    {
        var securityConfiguration = new SecurityConfiguration
        {
            SecretKey = "test-secret-key-with-enough-length-1234567890",
            Issuer = "external-system-issuer",
            PackageName = "external-system-audience"
        };

        var services = new ServiceCollection();
        services.AddDualExternalSystemAuthentication(securityConfiguration);

        using var provider = services.BuildServiceProvider();
        tokenOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(CustomAuthSchemes.ExternalSystemTokenAuthentication);
        policyOptions = provider.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>()
            .Get(CustomAuthSchemes.DualExternalSystemScheme);
    }

    [Test]
    public void ExternalSystemTokenScheme_WhenConfigured_ShouldWireTokenValidationParametersFromConfiguration()
    {
        tokenOptions.TokenValidationParameters.Should().NotBeNull();
        tokenOptions.TokenValidationParameters.ValidIssuer.Should().Be("external-system-issuer");
        tokenOptions.TokenValidationParameters.ValidAudience.Should().Be("external-system-audience");
        tokenOptions.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        tokenOptions.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        tokenOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        tokenOptions.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        tokenOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
        tokenOptions.SaveToken.Should().BeTrue();
    }

    [Test]
    public void RequestWithBearerAuthorizationHeader_ForwardDefaultSelector_ShouldSelectTokenScheme()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer some-jwt";

        var selectedScheme = policyOptions.ForwardDefaultSelector(context);

        selectedScheme.Should().Be(CustomAuthSchemes.ExternalSystemTokenAuthentication);
    }

    [Test]
    public void RequestWithoutAuthorizationHeader_ForwardDefaultSelector_ShouldSelectCertificateScheme()
    {
        var context = new DefaultHttpContext();

        var selectedScheme = policyOptions.ForwardDefaultSelector(context);

        selectedScheme.Should().Be(CustomAuthSchemes.CertificateAuthentication);
    }

    [Test]
    public void RequestWithNonBearerAuthorizationHeader_ForwardDefaultSelector_ShouldSelectCertificateScheme()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Basic some-credentials";

        var selectedScheme = policyOptions.ForwardDefaultSelector(context);

        selectedScheme.Should().Be(CustomAuthSchemes.CertificateAuthentication);
    }

    [Test]
    public void RequestWithLowercaseBearerAuthorizationHeader_ForwardDefaultSelector_ShouldSelectTokenScheme()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "bearer some-jwt";

        var selectedScheme = policyOptions.ForwardDefaultSelector(context);

        selectedScheme.Should().Be(CustomAuthSchemes.ExternalSystemTokenAuthentication);
    }

    [Test]
    public void DualExternalSystemAuthentication_WhenConfigured_ShouldSetDualSchemeAsDefault()
    {
        var securityConfiguration = new SecurityConfiguration
        {
            SecretKey = "test-secret-key-with-enough-length-1234567890",
            Issuer = "external-system-issuer",
            PackageName = "external-system-audience"
        };

        var services = new ServiceCollection();
        services.AddDualExternalSystemAuthentication(securityConfiguration);

        using var provider = services.BuildServiceProvider();
        var authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        authenticationOptions.DefaultAuthenticateScheme.Should().Be(CustomAuthSchemes.DualExternalSystemScheme);
        authenticationOptions.DefaultChallengeScheme.Should().Be(CustomAuthSchemes.DualExternalSystemScheme);
        authenticationOptions.DefaultScheme.Should().Be(CustomAuthSchemes.DualExternalSystemScheme);
    }
}
