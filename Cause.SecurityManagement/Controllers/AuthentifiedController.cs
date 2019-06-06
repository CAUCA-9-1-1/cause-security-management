using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Cause.SecurityManagement.Controllers
{
    [Route("api/[controller]")]
    public class AuthentifiedController : Controller
    {
        protected Guid GetUserId()
        {
            var id = User.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;

            if (Guid.TryParse(id, out Guid userId))
                return userId;
            return Guid.Empty;
        }
    }
}