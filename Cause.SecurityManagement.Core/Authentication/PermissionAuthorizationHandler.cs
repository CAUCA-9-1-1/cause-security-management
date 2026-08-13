using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Decides the permission gate. Administrators pass without a permission lookup when the
/// requirement allows them and are denied when it does not. RegularUsers pass only when they
/// hold the named permission. Every other principal is denied without a database read.
/// Fails closed: the requirement is satisfied only on an explicit positive match.
/// </summary>
internal sealed class PermissionAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    IServiceScopeFactory scopeFactory)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return;

        if (context.User.IsInRole(SecurityRoles.Administrator))
        {
            if (requirement.AllowAdministrator)
                context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole(SecurityRoles.User))
            return;

        if (!Guid.TryParse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sid), out var userId))
            return;

        if (await HasPermissionAsync(userId, requirement.Tag))
            context.Succeed(requirement);
    }

    private async Task<bool> HasPermissionAsync(Guid userId, string tag)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;

        if (httpContext?.RequestServices is not null)
            return await httpContext.RequestServices.GetRequiredService<ScopedPermissionCache>()
                .HasPermissionAsync(userId, tag, cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ScopedPermissionCache>()
            .HasPermissionAsync(userId, tag, cancellationToken);
    }
}
