namespace Cause.SecurityManagement.Models.DataTransferObjects;

public record TokenRefreshResult
{
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
}
