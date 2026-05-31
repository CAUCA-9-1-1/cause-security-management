using Cause.SecurityManagement.Core.Authentication.MultiFactor;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Integration.Tests.Infrastructure;

/// <summary>
/// Captures the last sent validation code so tests can retrieve and use it for verification.
/// </summary>
public class TestValidationCodeSender : IAuthenticationValidationCodeSender<TestUser>
{
    public string? LastSentCode { get; private set; }
    public TestUser? LastRecipient { get; private set; }
    public DateTime? LastExpiry { get; private set; }

    // Called when the system generates the code internally (no explicit code param)
    public Task SendCodeAsync(TestUser user)
    {
        LastRecipient = user;
        return Task.CompletedTask;
    }

    public Task SendCodeAsync(TestUser user, string code, DateTime expiresOn,
        ValidationCodeCommunicationType communicationType = ValidationCodeCommunicationType.Sms)
    {
        LastSentCode = code;
        LastRecipient = user;
        LastExpiry = expiresOn;
        return Task.CompletedTask;
    }
}
