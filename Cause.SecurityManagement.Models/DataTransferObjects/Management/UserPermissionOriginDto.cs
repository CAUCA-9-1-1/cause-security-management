using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// One assignment contributing to a user's effective permission. <see cref="GroupId"/> and
    /// <see cref="GroupName"/> are filled only when <see cref="Kind"/> is
    /// <see cref="UserPermissionOriginKind.Group"/>.
    /// </summary>
    public sealed record UserPermissionOriginDto
    {
        public UserPermissionOriginKind Kind { get; set; }
        public Guid? GroupId { get; set; }
        public string GroupName { get; set; }
        public bool IsAllowed { get; set; }
    }
}
