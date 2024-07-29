using System;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Repositories;

public interface IAuthenticableEntityRepository<out T> where T : IAuthenticableEntity
{
    T GetEntityById(Guid entityId);
    T GetEntityWithTemporaryPassword(string userName, string password);
    T GetEntity(string userName, string password);
    bool HasToken(Guid entityId, string refreshToken);
}