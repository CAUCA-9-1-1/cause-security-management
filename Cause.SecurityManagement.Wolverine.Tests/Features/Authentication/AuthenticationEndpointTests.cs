using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Wolverine.Features.Authentication;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Wolverine.Tests.Features.Authentication;

[TestFixture]
public class GetAuthenticationStateEndpointTests
{
    private IEntityAuthenticator authenticator = null!;

    [SetUp]
    public void SetUp() => authenticator = Substitute.For<IEntityAuthenticator>();

    [Test]
    public void WhenLoggedIn_ShouldReturnTrue()
    {
        authenticator.IsLoggedIn("tok").Returns(true);

        var result = GetAuthenticationStateEndpoint.Handle(new AuthenticationStateRequest { RefreshToken = "tok" }, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<bool>>()
            .Which.Value.Should().BeTrue();
    }

    [Test]
    public void WhenNotLoggedIn_ShouldReturnFalse()
    {
        authenticator.IsLoggedIn("tok").Returns(false);

        var result = GetAuthenticationStateEndpoint.Handle(new AuthenticationStateRequest { RefreshToken = "tok" }, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<bool>>()
            .Which.Value.Should().BeFalse();
    }
}

[TestFixture]
public class RecoverAccountEndpointTests
{
    private IEntityAuthenticator authenticator = null!;

    [SetUp]
    public void SetUp() => authenticator = Substitute.For<IEntityAuthenticator>();

    [Test]
    public async Task WhenRecovering_ShouldDelegateAndReturnNoContent()
    {
        var result = await RecoverAccountEndpoint.HandleAsync(new AccountRecoveryRequest { Email = "x@y.com" }, authenticator);

        await authenticator.Received(1).RecoverAccountAsync("x@y.com");
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }
}

[TestFixture]
public class ValidateRecoverAccountEndpointTests
{
    private IEntityAuthenticator authenticator = null!;

    [SetUp]
    public void SetUp() => authenticator = Substitute.For<IEntityAuthenticator>();

    [Test]
    public async Task WithValidCode_WhenValidating_ShouldReturnLoginResult()
    {
        var expected = new LoginResult { MustChangePassword = true };
        authenticator.ValidateAccountRecoveryAsync("user", "code").Returns(expected);

        var result = await ValidateRecoverAccountEndpoint.HandleAsync(
            new AccountRecoveryValidationRequest { Email = "user", ValidationCode = "code" }, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<LoginResult>>()
            .Which.Value.Should().Be(expected);
    }

    [Test]
    public async Task WithInvalidCode_WhenValidating_ShouldReturnBadRequest()
    {
        authenticator.ValidateAccountRecoveryAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((LoginResult?)null);

        var result = await ValidateRecoverAccountEndpoint.HandleAsync(
            new AccountRecoveryValidationRequest { Email = "user", ValidationCode = "bad" }, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest>();
    }
}

[TestFixture]
public class SetPasswordEndpointTests
{
    private IEntityAuthenticator authenticator = null!;

    [SetUp]
    public void SetUp() => authenticator = Substitute.For<IEntityAuthenticator>();

    [Test]
    public void WhenChangingPassword_ShouldDelegateAndReturnNoContent()
    {
        var result = SetPasswordEndpoint.Handle(new PasswordChangeRequest { NewPassword = "newPass" }, authenticator);

        authenticator.Received(1).ChangePassword("newPass");
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }
}
