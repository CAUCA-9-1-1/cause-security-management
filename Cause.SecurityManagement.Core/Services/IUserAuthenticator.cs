using Cause.SecurityManagement.Models;
using System;

namespace Cause.SecurityManagement.Core.Services;

public interface IUserAuthenticator : IEntityAuthenticator
{
    UserToken GenerateUserCreationToken(Guid userId);
}