using AwesomeAssertions;
using Cause.SecurityManagement.Core;

using Cause.SecurityManagement.Integration.Tests.Infrastructure;

using Cause.SecurityManagement.Integration.Tests.TestData;

using Cause.SecurityManagement.Core.Services;

using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Authentication;

[TestFixture]
public class AccountRecoveryFlowTests : IntegrationTestBase
{
    private IUserAuthenticator _authenticator = null!;
    private UserBuilder NewUser() => new(Context, TestConfiguration.PackageName);

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        SecurityManagementOptions.MultiFactorAuthenticationIsActivated = true;
        _authenticator = Resolve<IUserAuthenticator>();
    }

    [Test]
    public async Task RecoverAccount_WithUnknownEmail_CompletesWithoutError()
    {
        // Must not leak information about whether the account exists
        var act = () => _authenticator.RecoverAccountAsync("nobody@nowhere.com");

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RecoverAccount_WithKnownEmail_SendsRecoveryCode()
    {
        var user = NewUser().WithEmail("recover@test.com").Build();

        await _authenticator.RecoverAccountAsync(user.Email);

        ValidationCodeSender.LastSentCode.Should().NotBeNullOrEmpty();
        ValidationCodeSender.LastRecipient!.Id.Should().Be(user.Id);
    }

    [Test]
    public async Task RecoverAccount_WithKnownUsername_SendsRecoveryCode()
    {
        var builder = NewUser().WithEmail("recoverByUsername@test.com");
        var user = builder.Build();

        await _authenticator.RecoverAccountAsync(user.UserName);

        ValidationCodeSender.LastSentCode.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task ValidateAccountRecovery_WithValidCode_ReturnsRecoveryToken()
    {
        var user = NewUser().WithEmail("validate-recovery@test.com").Build();

        await _authenticator.RecoverAccountAsync(user.Email);
        var sentCode = ValidationCodeSender.LastSentCode!;

        var result = await _authenticator.ValidateAccountRecoveryAsync(user.Email, sentCode);

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.IdUser.Should().Be(user.Id);
    }

    [Test]
    public async Task ValidateAccountRecovery_WithWrongCode_ReturnsNull()
    {
        var user = NewUser().WithEmail("wrong-code@test.com").Build();
        await _authenticator.RecoverAccountAsync(user.Email);

        var result = await _authenticator.ValidateAccountRecoveryAsync(user.Email, "000000");

        result.Should().BeNull();
    }

    [Test]
    public async Task ValidateAccountRecovery_WithExpiredCode_ReturnsNull()
    {
        var user = NewUser().WithEmail("expired-recovery@test.com").Build();

        // Insert an expired code directly
        var expiredCode = new Models.UserValidationCode
        {
            IdUser = user.Id,
            Code = "654321",
            ExpiresOn = DateTime.Now.AddMinutes(-20),
            Type = Models.ValidationCode.ValidationCodeType.AccountRecovery,
        };
        Context.UserValidationCodes.Add(expiredCode);
        Context.SaveChanges();

        var result = await _authenticator.ValidateAccountRecoveryAsync(user.Email, "654321");

        result.Should().BeNull();
    }

    [Test]
    public async Task ValidateAccountRecovery_WithValidCode_TokenHasRecoveryRole()
    {
        var user = NewUser().WithEmail("recovery-role@test.com").Build();
        await _authenticator.RecoverAccountAsync(user.Email);
        var sentCode = ValidationCodeSender.LastSentCode!;

        var result = await _authenticator.ValidateAccountRecoveryAsync(user.Email, sentCode);

        result.Should().NotBeNull();
        var tokenReader = Resolve<ITokenReader>();
        var role = tokenReader.GetClaimValueFromExpiredToken(result!.AccessToken,
            System.Security.Claims.ClaimTypes.Role);
        role.Should().Be(SecurityRoles.UserRecovery);
    }
}
