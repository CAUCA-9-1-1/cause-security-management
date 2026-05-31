using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class MobileVersionEndpoints
{
    [WolverineGet("/api/Authentication/VersionValidator/{mobileVersion}/Latest")]
    [AllowAnonymous]
    public static IResult HandleLatest(string mobileVersion, IMobileVersionValidator validator)
        => Results.Ok(validator.IsMobileVersionLatest(mobileVersion));

    [WolverineGet("/api/Authentication/VersionValidator/{mobileVersion}")]
    [AllowAnonymous]
    public static IResult HandleValid(string mobileVersion, IMobileVersionValidator validator)
        => Results.Ok(validator.IsMobileVersionValid(mobileVersion));
}
