using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;

namespace Cause.SecurityManagement.Core.Services.Management
{
    /// <summary>
    /// Server-side paged search over active users, used to pick members while editing a group.
    /// </summary>
    public interface IUserSearchService
    {
        Task<UserSearchResultDto> SearchUsersAsync(UserSearchRequestDto request, CancellationToken cancellationToken = default);
    }
}
