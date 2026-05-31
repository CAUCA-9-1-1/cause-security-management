using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Services;
using Wolverine;

namespace Cause.SecurityManagement.Wolverine.Features.Sagas.Recovery;

/// <summary>
/// Tracks a user's account recovery flow across multiple HTTP requests.
/// The saga is keyed on <see cref="Id"/> = the trimmed username / email.
/// </summary>
/// <remarks>
/// Typical flow:
/// <list type="number">
///   <item>Recovery endpoint publishes <see cref="StartAccountRecovery"/> — this saga is created and a code is sent.</item>
///   <item>User submits the code; endpoint publishes <see cref="ValidateAccountRecovery"/> — saga completes and
///   returns a <see cref="LoginResult"/> with <see cref="LoginResult.MustChangePassword"/> set to <c>true</c>.</item>
/// </list>
/// </remarks>
public class AccountRecoverySaga : Saga
{
    /// <summary>Saga correlation key — the trimmed username or email used to initiate recovery.</summary>
    public string Id { get; set; }

    // -------------------------------------------------------------------------
    // Start — sends the recovery code
    // -------------------------------------------------------------------------

    public static async Task<AccountRecoverySaga> StartAsync(StartAccountRecovery message, IEntityAuthenticator authenticator)
    {
        await authenticator.RecoverAccountAsync(message.UsernameOrEmail);
        return new AccountRecoverySaga { Id = message.UsernameOrEmail.Trim() };
    }

    // -------------------------------------------------------------------------
    // Validate code → completes the saga and returns the recovery token
    // -------------------------------------------------------------------------

    public async Task<LoginResult?> HandleAsync(ValidateAccountRecovery message, IEntityAuthenticator authenticator)
    {
        var result = await authenticator.ValidateAccountRecoveryAsync(message.UsernameOrEmail, message.ValidationCode);
        MarkCompleted();
        return result;
    }
}
