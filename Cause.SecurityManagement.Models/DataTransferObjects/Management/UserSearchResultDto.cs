using System.Collections.Generic;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// Paged search response. <see cref="TotalCount"/> is the total number of matching users
    /// before paging, used by the UI paginator.
    /// </summary>
    public sealed record UserSearchResultDto
    {
        public List<UserForGroupDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
