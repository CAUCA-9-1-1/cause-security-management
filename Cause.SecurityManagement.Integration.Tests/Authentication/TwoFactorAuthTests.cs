using AwesomeAssertions;
using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Integration.Tests.TestData;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Authentication;

[TestFixture]
public class TwoFactorAuthTests : IntegrationTestBase
{
    private IUserAuthenticator _authenticator = null!;
    private UserBuilder NewUser() => new UserBuilder(Context, TestConfiguration.PackageName)
        .WithTwoFactorEnabled();

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        // Enable 2FA globally for all tests in this fixture
        SecurityManagementOptions.MultiFactorAuthenticationIsActivated = true;

        _authenticator = Resolve<IUserAuthenticator>();
    }

    [Test]
    public async Task UserWith2FAEnabled_WhenLoggingIn_ReturnsMustVerifyCode()
    {
        var builder = NewUser().WithPassword("Pass2FA!");
        var user = builder.Build();

        var result = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        result.Should().NotBeNull();
        result!.MustVerifyCode.Should().BeTrue();
        result.MustChangePassword.Should().BeFalse();
    }

    [Test]
    public async Task UserWith2FAEnabled_WhenLoggingIn_CodeSentToSender()
    {
        var builder = NewUser().WithPassword("Pass2FAv2!");
        var user = builder.Build();

        await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        ValidationCodeSender.LastSentCode.Should().NotBeNullOrEmpty();
        ValidationCodeSender.LastRecipient!.Id.Should().Be(user.Id);
    }

    [Test]
    public async Task UserWith2FAEnabled_WhenLoggingIn_PersistsValidationCodeInDatabase()
    {
        var builder = NewUser().WithPassword("Pass2FA!");
        var user = builder.Build();

        await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        var codeRepo = Resolve<IUserValidationCodeRepository>();
        var savedCode = codeRepo.GetLastCode(user.Id);
        savedCode.Should().NotBeNull();
        savedCode!.Type.Should().Be(ValidationCodeType.MultiFactorLogin);
        savedCode.ExpiresOn.Should().BeAfter(DateTime.Now);
    }

    [Test]
    public async Task UserWith2FAEnabled_WhenVerifyingValidCode_ReturnsFullToken()
    {
        var builder = NewUser().WithPassword("Pass2FA!");
        var user = builder.Build();

        // Login to trigger code send
        var loginResult = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);
        loginResult.Should().NotBeNull();

        // The ValidationCodeSender captured the code
        var sentCode = ValidationCodeSender.LastSentCode;
        sentCode.Should().NotBeNullOrEmpty();

        // Set the current user so ValidateMultiFactorCodeAsync knows who is verifying
        CurrentUserService.UserId = user.Id;

        var verifyResult = await _authenticator.ValidateMultiFactorCodeAsync(
            new ValidationInformation { ValidationCode = sentCode! });

        verifyResult.Should().NotBeNull();
        verifyResult!.MustVerifyCode.Should().BeFalse();
        verifyResult.AccessToken.Should().NotBeNullOrEmpty();
        verifyResult.IdUser.Should().Be(user.Id);
    }

    [Test]
    public async Task UserWith2FAEnabled_WhenVerifyingWrongCode_ThrowsInvalidValidationCodeException()
    {
        var builder = NewUser().WithPassword("Pass2FA!");
        var user = builder.Build();

        await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);
        CurrentUserService.UserId = user.Id;

        var act = () => _authenticator.ValidateMultiFactorCodeAsync(
            new ValidationInformation { ValidationCode = "000000" });

        await act.Should().ThrowAsync<InvalidValidationCodeException>();
    }

    [Test]
    public async Task UserWith2FAEnabled_WhenVerifyingExpiredCode_ReturnsInvalid()
    {
        var builder = NewUser().WithPassword("Pass2FA!");
        var user = builder.Build();

        // Directly insert an expired code
        var expiredCode = new UserValidationCode
        {
            IdUser = user.Id,
            Code = "123456",
            ExpiresOn = DateTime.Now.AddMinutes(-10), // already expired
            Type = ValidationCodeType.MultiFactorLogin,
        };
        Context.UserValidationCodes.Add(expiredCode);
        Context.SaveChanges();

        CurrentUserService.UserId = user.Id;

        var act = () => _authenticator.ValidateMultiFactorCodeAsync(
            new ValidationInformation { ValidationCode = "123456" });

        await act.Should().ThrowAsync<InvalidValidationCodeException>();
    }

    [Test]
    public async Task UserWith2FAEnabled_AfterValidCode_CodeIsDeletedFromDatabase()
    {
        var builder = NewUser().WithPassword("Pass2FA!");
        var user = builder.Build();

        await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);
        var sentCode = ValidationCodeSender.LastSentCode!;
        CurrentUserService.UserId = user.Id;

        await _authenticator.ValidateMultiFactorCodeAsync(
            new ValidationInformation { ValidationCode = sentCode });

        var codeRepo = Resolve<IUserValidationCodeRepository>();
        var remainingCode = codeRepo.GetLastCode(user.Id);
        remainingCode.Should().BeNull();
    }

    [Test]
    public async Task UserWith2FAAndPasswordReset_WhenLoggingIn_PasswordResetTakesPriority()
    {
        // When both flags are set: MustChangePassword=true, and MustVerifyCode=true (flag is set based
        // on user config alone), but the 2FA code is NOT sent because PasswordMustBeResetAfterLogin=true.
        var builder = NewUser().WithPassword("Pass2FA!").WithPasswordMustReset();
        var user = builder.Build();

        var result = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        result.Should().NotBeNull();
        result!.MustChangePassword.Should().BeTrue();
        // MustVerifyCode reflects user config (MFA on + 2FA enabled), not whether code was sent
        result.MustVerifyCode.Should().BeTrue();
        // Verify the code was NOT sent (the DB has no code for this user)
        ValidationCodeSender.LastSentCode.Should().BeNull();
    }
}
