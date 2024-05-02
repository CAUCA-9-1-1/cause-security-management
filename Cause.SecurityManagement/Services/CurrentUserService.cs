using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Repositories;

namespace Cause.SecurityManagement.Services
{
    public class CurrentUserService(
        ITokenReader reader,
        IHttpContextAccessor contextAccessor,
        IUserPermissionRepository userPermissionRepository)
        : ICurrentUserService
    {
        public Guid GetUserId()
        {
            var id = contextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;

            return Guid.TryParse(id, out var userId) ? userId : Guid.Empty;
        }

        public Guid? GetUserDeviceId()
        {
            var value = GetCustomClaimValue(AdditionalClaimsGenerator.DeviceIdType);
            return Guid.TryParse(value, out var id) ? id : null;
        }

        public string GetCustomClaimValue(string claimType)
        {
            return contextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(claim => claim.Type == claimType)?.Value;
        }

        public string GetUserIpAddress()
        {
            return contextAccessor.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }

        public async Task<List<AuthenticationUserPermission>> GetPermissionsAsync()
        {
            return await userPermissionRepository.GetUserPermissionsAsync(GetUserId());
        }
    }
}
