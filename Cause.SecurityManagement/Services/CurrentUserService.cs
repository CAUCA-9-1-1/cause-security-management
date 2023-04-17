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
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IUserPermissionRepository userPermissionRepository;

        public CurrentUserService(IHttpContextAccessor contextAccessor, IUserPermissionRepository userPermissionRepository)
        {
            this.contextAccessor = contextAccessor;
            this.userPermissionRepository = userPermissionRepository;
        }
        public Guid GetUserId()
        {
            var id = contextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sid)?.Value;

            if (Guid.TryParse(id, out var userId))
                return userId;
            return Guid.Empty;
        }

        public string GetUserIpAddress()
        {
            return contextAccessor.HttpContext?.Connection?.RemoteIpAddress?.MapToIPv4().ToString();
        }

        public async Task<List<AuthenticationUserPermission>> GetPermissionsAsync()
        {
            return await userPermissionRepository.GetUserPermissionsAsync(GetUserId());
        }
    }
}
