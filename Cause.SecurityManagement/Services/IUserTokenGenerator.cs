using Cause.SecurityManagement.Models;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services;

public interface IUserTokenGenerator : IEntityTokenGenerator<UserToken>;

public interface IEntityTokenGenerator<T> where T : BaseToken
{
    Task<T> GenerateEntityTokenAsync(IAuthenticableEntity entity, string role);
    T GenerateEntityToken(Guid entityId, string accessToken, string refreshToken, string role, Guid? specificDeviceId = null);
}