using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Repositories;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public class CurrentUserService(
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

        public Guid? GetExternalSystemId()
        {
            if (contextAccessor.HttpContext == null)
                return null;
            var hasExternalSystemRole = contextAccessor.HttpContext.User.IsInRole(SecurityRoles.ExternalSystem);
            if (hasExternalSystemRole)
            {
                var sidClaim = contextAccessor.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;
                if (Guid.TryParse(sidClaim, out var parsedId))
                {
                    return parsedId;
                }
            }
            return null;
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

        public string GetAuthentifiedUserIdentifier()
        {
            return GetCustomClaimValue(ClaimTypes.Name) ??
                   GetCustomClaimValue(JwtRegisteredClaimNames.Name) ?? 
                   GetCustomClaimValue(JwtRegisteredClaimNames.UniqueName);
        }

        public async Task<List<AuthenticationUserPermission>> GetPermissionsAsync()
        {
            return await userPermissionRepository.GetUserPermissionsAsync(GetUserId());
        }
    }
}
