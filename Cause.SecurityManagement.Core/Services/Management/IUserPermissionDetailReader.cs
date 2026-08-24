using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;

namespace Cause.SecurityManagement.Core.Services.Management
{
    /// <summary>
    /// Reads the whole permission catalog together with one user's effective state for each entry,
    /// for a management screen that shows why a user can or cannot do something.
    /// </summary>
    public interface IUserPermissionDetailReader
    {
        Task<List<UserPermissionDetailDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
