using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Authentication;

internal sealed class MultiJwtClaimsTransformer(IOptions<SecurityConfiguration> configuration, IOptions<KeycloakConfiguration> keycloakConfiguration = null) : IClaimsTransformation
{
    public const string AuthenticationSource = "auth_source";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(c => c.Type == AuthenticationSource))
        {
            return Task.FromResult(principal);
        }

        var claimsIdentity = new ClaimsIdentity();
        var issuer = principal.Identities
            .Select(identity => identity.FindFirst(JwtRegisteredClaimNames.Iss)?.Value)
            .FirstOrDefault();
        if (issuer == configuration.Value.Issuer)
        {
            claimsIdentity.AddClaim(new Claim(AuthenticationSource, CustomAuthSchemes.RegularUserAuthentication));
        }
        else if (issuer == keycloakConfiguration?.Value?.ValidIssuer)
        {
            claimsIdentity.AddClaim(new Claim(AuthenticationSource, CustomAuthSchemes.KeycloakAuthentication));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Administrator"));
        }
        principal.AddIdentity(claimsIdentity);
        return Task.FromResult(principal);
    }
}