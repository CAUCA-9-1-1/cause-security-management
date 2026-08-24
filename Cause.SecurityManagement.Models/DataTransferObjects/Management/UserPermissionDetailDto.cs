using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// One module permission from the catalog together with the user's effective state for it and the
    /// assignments that produced that state. Every catalog entry is returned, including those the user
    /// is assigned nowhere, which is what distinguishes this contract from
    /// <see cref="DataTransferObjects.UserMergedPermission"/>.
    ///
    /// <see cref="Name"/> is the label stored on the module permission row, so it displays in the
    /// language of the database rather than the caller's.
    /// </summary>
    public sealed record UserPermissionDetailDto
    {
        public Guid IdModulePermission { get; set; }
        public string Tag { get; set; }
        public string Name { get; set; }
        public PermissionStatus Status { get; set; }
        public List<UserPermissionOriginDto> Origins { get; set; } = [];
    }
}
