namespace Cause.SecurityManagement.Models.DataTransferObjects;

public record AuthenticationStateRequest
{
    public string RefreshToken { get; init; }
}