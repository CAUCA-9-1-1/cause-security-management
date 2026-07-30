using AwesomeAssertions;
using Cause.SecurityManagement.Authentication;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using NUnit.Framework;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;

namespace Cause.SecurityManagement.Tests.Authentication;

public class TokenAuthenticationExtensionsTests
{
    private const string RegularUserAuthenticationScheme = "RegularUserAuthentication";
    private const string KeycloakAuthenticationScheme = "KeycloakAuthentication";
    private const string ConsoleCertificateAuthenticationScheme = "ConsoleCertificateAuthentication";

    private SecurityConfiguration securityConfiguration;
    private TokenGenerator tokenGenerator;
    private MethodInfo getSchemeToUseMethod;

    [SetUp]
    public void SetUpTest()
    {
        securityConfiguration = new SecurityConfiguration
        {
            Issuer = "http://mytest.ca",
            PackageName = "CauseSecurityManagement",
            SecretKey = "RHzb3Z68KW9LanvjBoev2fupPzn94A3r"
        };
        tokenGenerator = new TokenGenerator(Options.Create(securityConfiguration));

        getSchemeToUseMethod = typeof(TokenAuthenticationExtensions).GetMethod(
            "GetSchemeToUse",
            BindingFlags.NonPublic | BindingFlags.Static);
    }

    private string InvokeGetSchemeToUse(KeycloakConfiguration keycloakConfiguration, HttpContext context)
    {
        try
        {
            return (string)getSchemeToUseMethod.Invoke(null, [keycloakConfiguration, context]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }

    private static HttpContext CreateContextWithAuthorization(string authorizationHeaderValue)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
        };
        if (authorizationHeaderValue != null)
            context.Request.Headers[HeaderNames.Authorization] = authorizationHeaderValue;
        return context;
    }

    [Test]
    public void MalformedBearerToken_WhenGettingSchemeToUse_ShouldFallBackToRegularUserWithoutThrowing()
    {
        var context = CreateContextWithAuthorization("Bearer not-a-jwt");
        var keycloakConfiguration = new KeycloakConfiguration { ShowDebugInfo = true, ValidIssuer = "http://mytest.ca" };

        Action action = () => InvokeGetSchemeToUse(keycloakConfiguration, context);

        action.Should().NotThrow();
        InvokeGetSchemeToUse(keycloakConfiguration, context).Should().Be(RegularUserAuthenticationScheme);
    }

    [Test]
    public void ValidKeycloakToken_WhenGettingSchemeToUse_ShouldReturnKeycloakAuthentication()
    {
        var token = tokenGenerator.GenerateAccessToken("someUserId", "someUser", SecurityRoles.Administrator);
        var context = CreateContextWithAuthorization($"Bearer {token}");
        var keycloakConfiguration = new KeycloakConfiguration { ValidIssuer = securityConfiguration.Issuer };

        var result = InvokeGetSchemeToUse(keycloakConfiguration, context);

        result.Should().Be(KeycloakAuthenticationScheme);
    }

    [Test]
    public void ValidRegularUserToken_WhenGettingSchemeToUse_ShouldReturnRegularUserAuthentication()
    {
        var token = tokenGenerator.GenerateAccessToken("someUserId", "someUser", SecurityRoles.User);
        var context = CreateContextWithAuthorization($"Bearer {token}");
        var keycloakConfiguration = new KeycloakConfiguration { ValidIssuer = "http://someOtherIssuer.ca" };

        var result = InvokeGetSchemeToUse(keycloakConfiguration, context);

        result.Should().Be(RegularUserAuthenticationScheme);
    }

    [Test]
    public void ValidConsoleToken_WhenGettingSchemeToUse_ShouldReturnConsoleCertificateAuthentication()
    {
        var token = tokenGenerator.GenerateAccessToken("someUserId", "someUser", SecurityRoles.ApiCertificate);
        var context = CreateContextWithAuthorization($"Bearer {token}");
        var keycloakConfiguration = new KeycloakConfiguration { ValidIssuer = "http://someOtherIssuer.ca" };

        var result = InvokeGetSchemeToUse(keycloakConfiguration, context);

        result.Should().Be(ConsoleCertificateAuthenticationScheme);
    }

    [Test]
    public void MissingAuthorizationHeader_WhenGettingSchemeToUse_ShouldReturnRegularUserAuthentication()
    {
        var context = CreateContextWithAuthorization(null);

        var result = InvokeGetSchemeToUse(null, context);

        result.Should().Be(RegularUserAuthenticationScheme);
    }

    [Test]
    public void NonBearerAuthorizationHeader_WhenGettingSchemeToUse_ShouldReturnRegularUserAuthentication()
    {
        var context = CreateContextWithAuthorization("Basic dXNlcjpwYXNz");

        var result = InvokeGetSchemeToUse(null, context);

        result.Should().Be(RegularUserAuthenticationScheme);
    }

    [TestCase("aaa.bbb.ccc")]
    [TestCase("eyJ.eyJ.sig")]
    [TestCase("a.b.c.d.e")]
    public void TokenShapedButUnparsable_WhenGettingSchemeToUse_ShouldFallBackToRegularUserWithoutThrowing(string malformedToken)
    {
        var context = CreateContextWithAuthorization($"Bearer {malformedToken}");
        var keycloakConfiguration = new KeycloakConfiguration { ShowDebugInfo = true, ValidIssuer = "http://mytest.ca" };

        Action action = () => InvokeGetSchemeToUse(keycloakConfiguration, context);

        action.Should().NotThrow();
        InvokeGetSchemeToUse(keycloakConfiguration, context).Should().Be(RegularUserAuthenticationScheme);
    }

    [Test]
    public void TokenExceedingMaximumSize_WhenGettingSchemeToUse_ShouldFallBackToRegularUserWithoutThrowing()
    {
        var maximumTokenSizeInBytes = new JwtSecurityTokenHandler().MaximumTokenSizeInBytes;
        var oversizedSegment = new string('a', maximumTokenSizeInBytes);
        var oversizedToken = $"{oversizedSegment}.{oversizedSegment}.{oversizedSegment}";
        var context = CreateContextWithAuthorization($"Bearer {oversizedToken}");

        Action action = () => InvokeGetSchemeToUse(null, context);

        action.Should().NotThrow();
        InvokeGetSchemeToUse(null, context).Should().Be(RegularUserAuthenticationScheme);
    }

    [Test]
    public void ValidTokenWithDebugInfoEnabled_WhenGettingSchemeToUse_ShouldLogClaimsAndReturnScheme()
    {
        var token = tokenGenerator.GenerateAccessToken("someUserId", "someUser", SecurityRoles.User);
        var context = CreateContextWithAuthorization($"Bearer {token}");
        var keycloakConfiguration = new KeycloakConfiguration { ShowDebugInfo = true, ValidIssuer = "http://someOtherIssuer.ca" };

        Action action = () => InvokeGetSchemeToUse(keycloakConfiguration, context);

        action.Should().NotThrow();
        InvokeGetSchemeToUse(keycloakConfiguration, context).Should().Be(RegularUserAuthenticationScheme);
    }
}
