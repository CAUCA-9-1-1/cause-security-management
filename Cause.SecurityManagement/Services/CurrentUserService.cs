using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;

namespace Cause.SecurityManagement.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor contextAccessor;

        public CurrentUserService(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }
        public Guid GetUserId()
        {
            var id = contextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;

            if (Guid.TryParse(id, out Guid userId))
                return userId;
            return Guid.Empty;
        }

        public string GetUserIpAddress()
        {
            return contextAccessor.HttpContext?.Connection?.RemoteIpAddress?.MapToIPv4().ToString();
        }
    }
}
