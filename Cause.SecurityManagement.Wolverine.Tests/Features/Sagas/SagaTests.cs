using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Wolverine.Features.Sagas.Login;
using Cause.SecurityManagement.Wolverine.Features.Sagas.Recovery;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Wolverine.Tests.Features.Sagas;

[TestFixture]
public class UserLoginSagaTests
{
    private IEntityAuthenticator authenticator = null!;

    [SetUp]
    public void SetUp() => authenticator = Substitute.For<IEntityAuthenticator>();

    [Test]
    public void Start_ShouldCreateSagaWithCorrectId()
    {
        var userId = Guid.NewGuid();

        var saga = UserLoginSaga.Start(new StartUserLogin(userId));

        saga.Id.Should().Be(userId);
        saga.IsCompleted().Should().BeFalse();
    }

    [Test]
    public async Task ResendCode_ShouldDelegateToAuthenticator()
    {
        var saga = new UserLoginSaga { Id = Guid.NewGuid() };
        var message = new ResendLoginCode(saga.Id, ValidationCodeCommunicationType.Voice);

        await saga.HandleAsync(message, authenticator);

        await authenticator.Received(1).SendNewCodeAsync(ValidationCodeCommunicationType.Voice);
    }

    [Test]
    public async Task VerifyCode_WithValidCode_ShouldReturnLoginResultAndMarkCompleted()
    {
        var saga = new UserLoginSaga { Id = Guid.NewGuid() };
        var expected = new LoginResult { AccessToken = "tok" };
        authenticator.ValidateMultiFactorCodeAsync(Arg.Any<ValidationInformation>()).Returns(expected);

        var result = await saga.HandleAsync(new VerifyLoginCode(saga.Id, "123456"), authenticator);

        result.Should().Be(expected);
        saga.IsCompleted().Should().BeTrue();
    }
}

[TestFixture]
public class AccountRecoverySagaTests
{
    private IEntityAuthenticator authenticator = null!;

    [SetUp]
    public void SetUp() => authenticator = Substitute.For<IEntityAuthenticator>();

    [Test]
    public async Task Start_ShouldSendRecoveryCodeAndCreateSagaWithCorrectId()
    {
        var message = new StartAccountRecovery("user@example.com");

        var saga = await AccountRecoverySaga.StartAsync(message, authenticator);

        await authenticator.Received(1).RecoverAccountAsync("user@example.com");
        saga.Id.Should().Be("user@example.com");
        saga.IsCompleted().Should().BeFalse();
    }

    [Test]
    public async Task ValidateCode_WithValidCode_ShouldReturnLoginResultAndMarkCompleted()
    {
        var saga = new AccountRecoverySaga { Id = "user@example.com" };
        var expected = new LoginResult { MustChangePassword = true };
        authenticator.ValidateAccountRecoveryAsync("user@example.com", "654321").Returns(expected);

        var result = await saga.HandleAsync(
            new ValidateAccountRecovery("user@example.com", "654321"), authenticator);

        result.Should().Be(expected);
        saga.IsCompleted().Should().BeTrue();
    }

    [Test]
    public async Task ValidateCode_WithInvalidCode_ShouldReturnNullAndMarkCompleted()
    {
        var saga = new AccountRecoverySaga { Id = "user@example.com" };
        authenticator.ValidateAccountRecoveryAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((LoginResult?)null);

        var result = await saga.HandleAsync(
            new ValidateAccountRecovery("user@example.com", "bad"), authenticator);

        result.Should().BeNull();
        saga.IsCompleted().Should().BeTrue();
    }
}
