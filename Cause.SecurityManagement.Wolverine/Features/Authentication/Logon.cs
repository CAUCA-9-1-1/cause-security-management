using System.Text;
using System.Text.Json;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Authentication;

public class LogonEndpoint
{
    private static readonly JsonSerializerOptions SerializationOptions = new() { PropertyNameCaseInsensitive = true };

    [WolverinePost("/api/Authentication/Logon")]
    [AllowAnonymous]
    public static async Task<IResult> HandleAsync(
        [FromHeader(Name = "auth")] string? authorizationHeader,
        LoginInformations? body,
        IEntityAuthenticator authenticator)
    {
        var login = GetLoginFromHeader(authorizationHeader) ?? body;
        if (login is null) return Results.Unauthorized();
        var result = await authenticator.LoginAsync(login.UserName, login.Password);
        return result is null ? Results.Unauthorized() : Results.Ok(result);
    }

    private static LoginInformations? GetLoginFromHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return null;
        var decoded = Uri.UnescapeDataString(Encoding.Default.GetString(Convert.FromBase64String(header)));
        return JsonSerializer.Deserialize<LoginInformations>(decoded, SerializationOptions);
    }
}
