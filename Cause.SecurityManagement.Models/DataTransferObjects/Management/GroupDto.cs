using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// Full group payload exchanged with the management UI. Used both as the GET result
    /// and as the POST (upsert) body. The client generates the <see cref="Id"/> for new
    /// groups and new permissions before posting.
    /// </summary>
    public sealed record GroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool AssignableByAllUsers { get; set; }
        public List<GroupPermissionDto> Permissions { get; set; } = new();
        public List<GroupUserDto> Users { get; set; } = new();
    }
}
