using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace Cause.SecurityManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KeycloakConfigurationController(IOptions<KeycloakConfiguration> options = null) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    [ProducesResponseType(typeof(KeycloakConfigurationForWeb), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Returns the configuration to access keycloak.",
        Description = "Returns keycloak url, realm and client."
    )]
    public ActionResult<KeycloakConfigurationForWeb> GetKeycloakConfiguration()
    {
        if (options == null)
            return Unauthorized("Keycloak not configured.");

        var configuration = options.Value;
        return Ok(new KeycloakConfigurationForWeb(configuration.Url, configuration.Realm, configuration.ClientId));
    }
}

public record struct KeycloakConfigurationForWeb(string Url, string Realm, string ClientId);