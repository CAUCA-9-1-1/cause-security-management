using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Keycloak;

public class GetKeycloakConfigurationEndpoint
{
    [WolverineGet("/api/KeycloakConfiguration")]
    [AllowAnonymous]
    public static IResult Handle(IOptions<KeycloakConfiguration>? options)
    {
        if (options?.Value is null) return Results.Unauthorized();
        var config = options.Value;
        return Results.Ok(new { config.Url, config.Realm, config.ClientId });
    }
}
