using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// An assignable module permission from the catalog. <see cref="Id"/> is the module-permission
    /// identifier referenced by <see cref="GroupPermissionDto.IdModulePermission"/>;
    /// <see cref="IdModulePermission"/> carries the same value.
    /// </summary>
    public sealed record PermissionDto
    {
        public Guid Id { get; set; }
        public Guid IdModulePermission { get; set; }
        public string Tag { get; set; }
        public string Name { get; set; }
    }
}
