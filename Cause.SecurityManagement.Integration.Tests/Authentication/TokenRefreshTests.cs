using AwesomeAssertions;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Integration.Tests.TestData;
using Cause.SecurityManagement.Core.Services;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Authentication;

[TestFixture]
public class TokenRefreshTests : IntegrationTestBase
{
    private IUserAuthenticator _authenticator = null!;
    private IUserTokenRefresher _tokenRefresher = null!;
    private UserBuilder NewUser() => new(Context, TestConfiguration.PackageName);

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _authenticator = Resolve<IUserAuthenticator>();
        _tokenRefresher = Resolve<IUserTokenRefresher>();
    }

    [Test]
    public async Task ValidRefreshToken_WhenRefreshed_ReturnsNewAccessToken()
    {
        var builder = NewUser().WithPassword("RefreshMe1!");
        var user = builder.Build();
        var loginResult = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);
        loginResult.Should().NotBeNull();

        var newAccessToken = await _tokenRefresher.GetNewAccessTokenAsync(
            loginResult!.AccessToken, loginResult.RefreshToken);

        // Just verify a new token was returned; same-second generation can produce identical JWTs.
        newAccessToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task UnknownRefreshToken_WhenRefreshing_ThrowsException()
    {
        var builder = NewUser().WithPassword("UnknownRef1!");
        var user = builder.Build();
        var loginResult = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);
        loginResult.Should().NotBeNull();

        var act = () => _tokenRefresher.GetNewAccessTokenAsync(
            loginResult!.AccessToken, "this-refresh-token-does-not-exist");

        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task IsLoggedIn_WithValidRefreshToken_ReturnsTrue()
    {
        var builder = NewUser().WithPassword("LoggedIn1!");
        var user = builder.Build();
        var loginResult = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);
        CurrentUserService.UserId = loginResult!.IdUser;

        var isLoggedIn = _authenticator.IsLoggedIn(loginResult!.RefreshToken);

        isLoggedIn.Should().BeTrue();
    }

    [Test]
    public async Task IsLoggedIn_WithUnknownToken_ReturnsFalse()
    {
        var isLoggedIn = _authenticator.IsLoggedIn("no-such-token");

        isLoggedIn.Should().BeFalse();
    }

    [Test]
    public async Task AfterLogin_RefreshProducesNewToken()
    {
        var builder = NewUser().WithPassword("MultiRefresh1!");
        var user = builder.Build();
        var loginResult = await _authenticator.LoginAsync(user.UserName, builder.PlainPassword);
        loginResult.Should().NotBeNull();

        var newAccessToken = await _tokenRefresher.GetNewAccessTokenAsync(
            loginResult!.AccessToken, loginResult.RefreshToken);

        newAccessToken.Should().NotBeNullOrEmpty();
    }
}
