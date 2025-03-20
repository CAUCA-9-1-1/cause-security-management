using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Cause.SecurityManagement.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MeController : ControllerBase
{
    [HttpGet]
    [AuthorizeForUserAndAdministratorRoles]
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