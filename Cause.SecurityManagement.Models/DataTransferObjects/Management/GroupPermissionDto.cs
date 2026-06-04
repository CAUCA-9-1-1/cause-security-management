using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// A group's permission override. <see cref="IdModulePermission"/> is a foreign key to the
    /// module permission catalog (<see cref="PermissionDto.Id"/>).
    /// </summary>
    public sealed record GroupPermissionDto
    {
        public Guid Id { get; set; }
        public Guid IdGroup { get; set; }
        public Guid IdModulePermission { get; set; }
        public bool IsAllowed { get; set; }
    }
}
