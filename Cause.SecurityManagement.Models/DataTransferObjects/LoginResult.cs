using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects;

public record LoginResult
{
    public string AuthorizationType { get; init; }
    public DateTime ExpiredOn { get; init; }
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
    public bool MustVerifyCode { get; init; }
    public bool MustChangePassword { get; init; }
    public Guid IdUser { get; init; }
    public string Name { get; init; }
    public string Username { get; init; }
}
