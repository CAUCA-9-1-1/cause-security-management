using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace Cause.SecurityManagement.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MeController : ControllerBase
{
    [HttpGet]
    [AuthorizeForUserAdministratorAndCertificateRoles]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The user claims and information")]
    [SwaggerOperation(
        Summary = "Retrieves the claims and information of the currently authenticated user",
        Description = "Requires one of the following roles: RegularUser, Administrator, Console")]
    public ActionResult Get()
    {
        return Ok(this.HttpContext.User.Claims
            .Select(claim => new
            {
                claim.Type,
                claim.Value
            })
            .ToList());
    }
}