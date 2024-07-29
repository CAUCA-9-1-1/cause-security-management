using Cause.SecurityManagement.Models;
using System.Threading.Tasks;
using System;

namespace Cause.SecurityManagement.Repositories;

public interface IEntityTokenRepository<T> where T : BaseToken
{
    T GetToken(Guid idUser, string refreshToken);
    void AddToken(T token);
    Task RemoveExistingTokenAsync(Guid entityId, string issuer);
    Task SaveChangesAsync();
}