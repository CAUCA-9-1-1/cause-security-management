namespace Cause.SecurityManagement.Models.DataTransferObjects;

public record LoginInformations
{
    public string UserName { get; init; }
    public string Password { get; init; }
}
