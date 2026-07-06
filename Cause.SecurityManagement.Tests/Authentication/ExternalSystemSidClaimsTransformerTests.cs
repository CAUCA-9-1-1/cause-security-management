using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Authentication;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication;

[TestFixture]
public class ExternalSystemSidClaimsTransformerTests
{
    private IClaimsTransformation transformer;

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

        var provider = services.BuildServiceProvider();
        transformer = provider.CreateScope().ServiceProvider.GetRequiredService<IClaimsTransformation>();
    }

    [Test]
    public async Task ExternalSystemWithOnlyJwtSidClaim_TransformAsync_ShouldAddClaimTypesSidWithSameValue()
    {
        var sid = Guid.NewGuid().ToString();
        var principal = CreatePrincipal(SecurityRoles.ExternalSystem, new Claim(JwtRegisteredClaimNames.Sid, sid));

        var transformed = await transformer.TransformAsync(principal);

        transformed.FindFirst(ClaimTypes.Sid)?.Value.Should().Be(sid);
    }

    [Test]
    public async Task ExternalSystemWithExistingClaimTypesSid_TransformAsync_ShouldLeaveItUnchanged()
    {
        var existingSid = Guid.NewGuid().ToString();
        var jwtSid = Guid.NewGuid().ToString();
        var principal = CreatePrincipal(
            SecurityRoles.ExternalSystem,
            new Claim(ClaimTypes.Sid, existingSid),
            new Claim(JwtRegisteredClaimNames.Sid, jwtSid));

        var transformed = await transformer.TransformAsync(principal);

        transformed.FindAll(ClaimTypes.Sid).Should().ContainSingle().Which.Value.Should().Be(existingSid);
    }

    [Test]
    public async Task NonExternalSystemPrincipal_TransformAsync_ShouldNotAddClaimTypesSid()
    {
        var principal = CreatePrincipal(SecurityRoles.User, new Claim(JwtRegisteredClaimNames.Sid, Guid.NewGuid().ToString()));

        var transformed = await transformer.TransformAsync(principal);

        transformed.HasClaim(claim => claim.Type == ClaimTypes.Sid).Should().BeFalse();
    }

    [Test]
    public async Task ExternalSystemWithOnlyClaimTypesSid_TransformAsync_ShouldAddJwtSidClaimWithSameValue()
    {
        var sid = Guid.NewGuid().ToString();
        var principal = CreatePrincipal(SecurityRoles.ExternalSystem, new Claim(ClaimTypes.Sid, sid));

        var transformed = await transformer.TransformAsync(principal);

        transformed.FindFirst(JwtRegisteredClaimNames.Sid)?.Value.Should().Be(sid);
    }

    [Test]
    public async Task ExternalSystemWithNeitherSidClaim_TransformAsync_ShouldReturnPrincipalUnchanged()
    {
        var principal = CreatePrincipal(SecurityRoles.ExternalSystem);

        var transformed = await transformer.TransformAsync(principal);

        transformed.HasClaim(claim => claim.Type == ClaimTypes.Sid).Should().BeFalse();
        transformed.HasClaim(claim => claim.Type == JwtRegisteredClaimNames.Sid).Should().BeFalse();
    }

    [Test]
    public async Task ExternalSystemWithOnlyClaimTypesSid_TransformAsyncTwice_ShouldAddNoAdditionalClaimsOnSecondCall()
    {
        var sid = Guid.NewGuid().ToString();
        var principal = CreatePrincipal(SecurityRoles.ExternalSystem, new Claim(ClaimTypes.Sid, sid));

        var firstTransform = await transformer.TransformAsync(principal);
        var secondTransform = await transformer.TransformAsync(firstTransform);

        secondTransform.FindAll(ClaimTypes.Sid).Should().ContainSingle().Which.Value.Should().Be(sid);
        secondTransform.FindAll(JwtRegisteredClaimNames.Sid).Should().ContainSingle().Which.Value.Should().Be(sid);
    }

    private static ClaimsPrincipal CreatePrincipal(string role, params Claim[] additionalClaims)
    {
        var claims = new[] { new Claim(ClaimTypes.Role, role) }.Concat(additionalClaims);
        var identity = new ClaimsIdentity(claims, "TestAuthenticationType");
        return new ClaimsPrincipal(identity);
    }
}
