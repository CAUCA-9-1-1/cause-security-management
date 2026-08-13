using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Authentication;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class MultiJwtClaimsTransformerTests
{
    private const string RegularUserIssuer = "https://regular-user-issuer";
    private const string KeycloakIssuer = "https://keycloak-test";

    private IOptions<SecurityConfiguration> configuration;

    [SetUp]
    public void SetUp()
    {
        configuration = Options.Create(new SecurityConfiguration { Issuer = RegularUserIssuer });
    }

    private static ClaimsPrincipal CertificatePrincipalWithoutIssuer()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, SecurityRoles.ExternalSystem),
            new(ClaimTypes.Sid, System.Guid.NewGuid().ToString()),
        };
        var identity = new ClaimsIdentity(claims, "CertificateAuthenticationHandler");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal PrincipalWithIssuer(string issuer)
    {
        var identity = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Iss, issuer)]);
        return new ClaimsPrincipal(identity);
    }

    [Test]
    public async Task PrincipalWithoutIssuerClaim_WhenTransforming_ShouldNotGrantAdministrator()
    {
        var transformer = new MultiJwtClaimsTransformer(configuration);

        var result = await transformer.TransformAsync(CertificatePrincipalWithoutIssuer());

        result.IsInRole(SecurityRoles.Administrator).Should().BeFalse(
            "a certificate-authenticated principal carrying no issuer claim must never be escalated to Administrator");
    }

    [Test]
    public async Task PrincipalWithoutIssuerClaim_WhenKeycloakIsConfigured_ShouldNotGrantAdministrator()
    {
        var keycloakConfiguration = Options.Create(new KeycloakConfiguration { ValidIssuer = KeycloakIssuer });
        var transformer = new MultiJwtClaimsTransformer(configuration, keycloakConfiguration);

        var result = await transformer.TransformAsync(CertificatePrincipalWithoutIssuer());

        result.IsInRole(SecurityRoles.Administrator).Should().BeFalse(
            "a certificate-authenticated principal carrying no issuer claim must never be escalated to Administrator");
    }

    [Test]
    public async Task KeycloakIssuer_WhenTransforming_ShouldGrantAdministrator()
    {
        var keycloakConfiguration = Options.Create(new KeycloakConfiguration { ValidIssuer = KeycloakIssuer });
        var transformer = new MultiJwtClaimsTransformer(configuration, keycloakConfiguration);

        var result = await transformer.TransformAsync(PrincipalWithIssuer(KeycloakIssuer));

        result.IsInRole(SecurityRoles.Administrator).Should().BeTrue();
    }

    [Test]
    public async Task RegularUserIssuer_WhenTransforming_ShouldNotGrantAdministrator()
    {
        var transformer = new MultiJwtClaimsTransformer(configuration);

        var result = await transformer.TransformAsync(PrincipalWithIssuer(RegularUserIssuer));

        result.IsInRole(SecurityRoles.Administrator).Should().BeFalse();
        result.HasClaim(MultiJwtClaimsTransformer.AuthenticationSource, CustomAuthSchemes.RegularUserAuthentication)
            .Should().BeTrue();
    }

    [Test]
    public async Task PrincipalAlreadyCarryingAuthSource_WhenTransforming_ShouldBeReturnedUnchanged()
    {
        var transformer = new MultiJwtClaimsTransformer(configuration);
        var identity = new ClaimsIdentity(
        [
            new Claim(MultiJwtClaimsTransformer.AuthenticationSource, CustomAuthSchemes.KeycloakAuthentication),
        ]);
        var principal = new ClaimsPrincipal(identity);

        var result = await transformer.TransformAsync(principal);

        result.Should().BeSameAs(principal);
        result.Identities.Should().ContainSingle();
    }
}
