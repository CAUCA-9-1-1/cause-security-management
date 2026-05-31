using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects;

public record AuthenticationUserPermission
{
    public Guid IdModulePermission { get; init; }
    public string Tag { get; init; }
    public bool IsAllowed { get; init; }
}
