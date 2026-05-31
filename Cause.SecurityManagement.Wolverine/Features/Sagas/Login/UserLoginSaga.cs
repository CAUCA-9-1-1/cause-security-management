using System;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Core.Services;
using Wolverine;

namespace Cause.SecurityManagement.Wolverine.Features.Sagas.Login;

/// <summary>
/// Tracks a user's multi-factor authentication login flow across multiple HTTP requests.
/// The saga is keyed on <see cref="Id"/> = user ID.
/// </summary>
/// <remarks>
/// Typical flow:
/// <list type="number">
///   <item>Login endpoint calls <c>LoginAsync</c> and sees <see cref="LoginResult.MustVerifyCode"/> is true.</item>
///   <item>Endpoint publishes <see cref="StartUserLogin"/> — this saga is created.</item>
///   <item>User submits the code; endpoint publishes <see cref="VerifyLoginCode"/> — saga completes and returns full <see cref="LoginResult"/>.</item>
/// </list>
/// </remarks>
public class UserLoginSaga : Saga
{
    /// <summary>Saga correlation key — equals the authenticated user's ID.</summary>
    public Guid Id { get; set; }

    // -------------------------------------------------------------------------
    // Start
    // -------------------------------------------------------------------------

    public static UserLoginSaga Start(StartUserLogin message)
        => new() { Id = message.UserId };

    // -------------------------------------------------------------------------
    // Resend code
    // -------------------------------------------------------------------------

    public async Task HandleAsync(ResendLoginCode message, IEntityAuthenticator authenticator)
    {
        await authenticator.SendNewCodeAsync(message.CommunicationType);
    }

    // -------------------------------------------------------------------------
    // Verify code → completes the saga and returns the full token
    // -------------------------------------------------------------------------

    public async Task<LoginResult?> HandleAsync(VerifyLoginCode message, IEntityAuthenticator authenticator)
    {
        var result = await authenticator.ValidateMultiFactorCodeAsync(
            new ValidationInformation { ValidationCode = message.Code });

        MarkCompleted();
        return result;
    }
}
