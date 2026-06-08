namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>Result of a group-name availability check.</summary>
    public sealed record GroupNameAvailabilityDto
    {
        public bool IsAvailable { get; init; }
    }
}
