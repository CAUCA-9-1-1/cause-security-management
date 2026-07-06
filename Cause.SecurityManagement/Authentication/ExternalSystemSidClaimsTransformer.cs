using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Cause.SecurityManagement.Core;
using Microsoft.AspNetCore.Authentication;

namespace Cause.SecurityManagement.Authentication;

internal sealed class ExternalSystemSidClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (!IsExternalSystem(principal))
        {
            return Task.FromResult(principal);
        }

        var sid = GetSid(principal);
        if (string.IsNullOrEmpty(sid))
        {
            return Task.FromResult(principal);
        }

        return Task.FromResult(EnsureBothSidClaimsArePresent(principal, sid));
    }

    private static ClaimsPrincipal EnsureBothSidClaimsArePresent(ClaimsPrincipal principal, string sid)
    {
        var isMissingClaimTypesSid = !principal.HasClaim(claim => claim.Type == ClaimTypes.Sid);
        var isMissingJwtSid = !principal.HasClaim(claim => claim.Type == JwtRegisteredClaimNames.Sid);
        if (!isMissingClaimTypesSid && !isMissingJwtSid)
        {
            return principal;
        }

        var clonedPrincipal = principal.Clone();
        var identity = clonedPrincipal.Identities.First();
        if (isMissingClaimTypesSid)
        {
            identity.AddClaim(new Claim(ClaimTypes.Sid, sid));
        }
        if (isMissingJwtSid)
        {
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sid, sid));
        }
        return clonedPrincipal;
    }

    private static string? GetSid(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Sid)?.Value ?? principal.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;
    }

    private static bool IsExternalSystem(ClaimsPrincipal principal)
    {
        return principal.HasClaim(claim => claim.Type == ClaimTypes.Role && claim.Value == SecurityRoles.ExternalSystem);
    }
}
