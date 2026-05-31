using AwesomeAssertions;
using Cause.SecurityManagement.Core;

using Cause.SecurityManagement.Integration.Tests.Infrastructure;

using Cause.SecurityManagement.Integration.Tests.TestData;

using Cause.SecurityManagement.Core.Services;

using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Authentication;

[TestFixture]
public class UserLoginFlowTests : IntegrationTestBase
{
    private IUserAuthenticator _authenticator = null!;
    private UserBuilder NewUser() => new(Context, TestConfiguration.PackageName);

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _authenticator = Resolve<IUserAuthenticator>();
    }

    [Test]
    public async Task UnknownUser_WhenLoggingIn_ReturnsNull()
    {
        var result = await _authenticator.LoginAsync("nobody", "wrongpass");

        result.Should().BeNull();
    }

    [Test]
    public async Task KnownUserWithCorrectPassword_WhenLoggingIn_ReturnsFullToken()
    {
        var builder = NewUser().WithPassword("MyPassword1!");
        var user = builder.Build();

        var result = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.MustVerifyCode.Should().BeFalse();
        result.MustChangePassword.Should().BeFalse();
        result.IdUser.Should().Be(user.Id);
    }

    [Test]
    public async Task KnownUserWithWrongPassword_WhenLoggingIn_ReturnsNull()
    {
        var user = NewUser().Build();

        var result = await _authenticator.LoginAsync(user.UserName, "wrong-password");

        result.Should().BeNull();
    }

    [Test]
    public async Task InactiveUser_WhenLoggingIn_ReturnsNull()
    {
        var builder = NewUser().IsInactive().WithPassword("Pass1!");
        var user = builder.Build();

        var result = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        result.Should().BeNull();
    }

    [Test]
    public async Task UserWithTemporaryPassword_WhenLoggingIn_ReturnsMustChangePassword()
    {
        // Temporary passwords are stored as-is (raw) in the database — not hashed
        const string rawTempPassword = "Temp1234!";
        var user = NewUser()
            .WithRawPassword()
            .WithPassword(rawTempPassword)
            .WithPasswordMustReset()
            .Build();

        var result = await _authenticator.LoginAsync(user.UserName, rawTempPassword);

        result.Should().NotBeNull();
        result!.MustChangePassword.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task UserWithPasswordMustBeResetFlag_WhenLoggingInWithRealPassword_ReturnsMustChangePassword()
    {
        // Real (encoded) password but the flag is set — not a temp password path
        var builder = NewUser().WithPassword("Encoded1!").WithPasswordMustReset();
        var user = builder.Build();

        var result = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        result.Should().NotBeNull();
        result!.MustChangePassword.Should().BeTrue();
        result.MustVerifyCode.Should().BeFalse();
    }

    [Test]
    public async Task UserWithPasswordMustBeResetFlag_WhenLoggingIn_TokenHasPasswordSetupRole()
    {
        var builder = NewUser().WithPassword("Encoded1!").WithPasswordMustReset();
        var user = builder.Build();

        var result = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        result.Should().NotBeNull();
        // The access token embeds the role — verify by reading the claim
        var tokenReader = Resolve<ITokenReader>();
        var role = tokenReader.GetClaimValueFromExpiredToken(result!.AccessToken,
            System.Security.Claims.ClaimTypes.Role);
        role.Should().Be(SecurityRoles.UserPasswordSetup);
    }

    [Test]
    public async Task UserWithCorrectPassword_AfterLogin_HasTokenSavedInDatabase()
    {
        var builder = NewUser().WithPassword("Pass1!");
        var user = builder.Build();

        var result = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);

        result.Should().NotBeNull();
        CurrentUserService.UserId = result!.IdUser;
        var tokenExists = _authenticator.IsLoggedIn(result.RefreshToken);
        tokenExists.Should().BeTrue();
    }

    [Test]
    public async Task ChangePassword_WhenCalledWithPasswordSetupRole_ClearsResetFlag()
    {
        var builder = NewUser().WithPassword("OldPass1!").WithPasswordMustReset();
        var user = builder.Build();

        // Login to get a UserPasswordSetup token
        var loginResult = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);
        loginResult.Should().NotBeNull();

        // Configure current user so ChangePassword knows who is changing
        CurrentUserService.UserId = user.Id;

        _authenticator.ChangePassword("NewPass1!");

        // Re-login with the new password should succeed and not require password change
        var refreshedContext = DatabaseFixture.CreateContext();
        refreshedContext.Users.Find(user.Id)!.PasswordMustBeResetAfterLogin.Should().BeFalse();
        await refreshedContext.DisposeAsync();
    }
}
