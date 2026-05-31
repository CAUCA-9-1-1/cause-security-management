namespace Cause.SecurityManagement.Models.DataTransferObjects;

public record UserMergedPermission
{
    public string FeatureName { get; init; }
    public bool Access { get; init; }
}
