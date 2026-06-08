using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// Paged search request over active users. <see cref="Query"/> matches first or last name,
    /// <see cref="ExcludedUserIds"/> removes already-selected users from the results.
    /// </summary>
    public sealed record UserSearchRequestDto
    {
        public string Query { get; set; }
        public int Skip { get; set; }
        public int Top { get; set; }
        public List<Guid> ExcludedUserIds { get; set; } = new();
    }
}
