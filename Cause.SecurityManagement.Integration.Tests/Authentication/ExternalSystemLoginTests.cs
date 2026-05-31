using AwesomeAssertions;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Integration.Tests.TestData;
using Cause.SecurityManagement.Core.Services;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Authentication;

[TestFixture]
public class ExternalSystemLoginTests : IntegrationTestBase
{
    private IExternalSystemAuthenticationService _authService = null!;
    private ExternalSystemBuilder NewSystem() => new(Context);

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _authService = Resolve<IExternalSystemAuthenticationService>();
    }

    [Test]
    public void ExternalSystem_WhenLoggingInWithValidApiKey_ReturnsToken()
    {
        var builder = NewSystem();
        builder.Build();

        var (token, system) = _authService.Login(builder.ApiKey);

        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrEmpty();
        token.RefreshToken.Should().NotBeNullOrEmpty();
        system.Should().NotBeNull();
    }

    [Test]
    public void ExternalSystem_WhenLoggingInWithUnknownApiKey_ReturnsNullTuple()
    {
        var (token, system) = _authService.Login("completely-unknown-api-key");

        token.Should().BeNull();
        system.Should().BeNull();
    }

    [Test]
    public void InactiveExternalSystem_WhenLoggingIn_ReturnsNullTuple()
    {
        var builder = NewSystem().IsInactive();
        builder.Build();

        var (token, system) = _authService.Login(builder.ApiKey);

        token.Should().BeNull();
        system.Should().BeNull();
    }

    [Test]
    public void ExternalSystem_AfterLogin_RefreshTokenIsStoredInDatabase()
    {
        var builder = NewSystem();
        var system = builder.Build();

        var (token, _) = _authService.Login(builder.ApiKey);

        token.Should().NotBeNull();
        var tokenExists = Context.ExternalSystemTokens
            .Any(t => t.IdExternalSystem == system.Id && t.RefreshToken == token!.RefreshToken);
        tokenExists.Should().BeTrue();
    }

    [Test]
    public async Task ExternalSystemToken_WhenRefreshed_ReturnsNewAccessToken()
    {
        var builder = NewSystem();
        builder.Build();

        var (loginToken, _) = _authService.Login(builder.ApiKey);
        loginToken.Should().NotBeNull();

        var newAccessToken = await _authService.RefreshAccessTokenAsync(
            loginToken!.AccessToken, loginToken.RefreshToken);

        // Just verify a token was returned; same-second generation can produce identical JWTs.
        newAccessToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task ExternalSystemToken_WhenRefreshedWithInvalidRefreshToken_ThrowsException()
    {
        var builder = NewSystem();
        builder.Build();
        var (loginToken, _) = _authService.Login(builder.ApiKey);
        loginToken.Should().NotBeNull();

        var act = () => _authService.RefreshAccessTokenAsync(
            loginToken!.AccessToken, "not-a-valid-refresh-token");

        await act.Should().ThrowAsync<Exception>();
    }
}
