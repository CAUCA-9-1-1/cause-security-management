using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication.MultiFactor;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Wolverine.Features.Authentication;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Cause.SecurityManagement.Wolverine.Tests.Features.Authentication;

[TestFixture]
public class SendValidationCodeEndpointTests
{
    private IEntityAuthenticator authenticator;

    [SetUp]
    public void SetUp() => authenticator = Substitute.For<IEntityAuthenticator>();

    [Test]
    public async Task WhenSendingCode_ShouldReturnOk()
    {
        var result = await SendValidationCodeEndpoint.HandleAsync(ValidationCodeCommunicationType.Sms, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok>();
        await authenticator.Received(1).SendNewCodeAsync(ValidationCodeCommunicationType.Sms);
    }

    [Test]
    public async Task WhenCodeNotFound_ShouldReturnUnauthorized()
    {
        authenticator.SendNewCodeAsync(Arg.Any<ValidationCodeCommunicationType>())
            .ThrowsAsync(new UserValidationCodeNotFoundException());

        var result = await SendValidationCodeEndpoint.HandleAsync(ValidationCodeCommunicationType.Sms, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }
}

[TestFixture]
public class VerifyValidationCodeEndpointTests
{
    private IEntityAuthenticator authenticator;

    [SetUp]
    public void SetUp() => authenticator = Substitute.For<IEntityAuthenticator>();

    [Test]
    public async Task WithValidCode_WhenVerifying_ShouldReturnLoginResult()
    {
        var expected = new LoginResult { AccessToken = "tok" };
        var body = new ValidationInformation { ValidationCode = "123456" };
        authenticator.ValidateMultiFactorCodeAsync(body).Returns(expected);

        var result = await VerifyValidationCodeEndpoint.HandleAsync(body, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<LoginResult>>()
            .Which.Value.Should().Be(expected);
    }

    [Test]
    public async Task WithInvalidCode_WhenVerifying_ShouldReturnBadRequest()
    {
        var body = new ValidationInformation { ValidationCode = "000000" };
        authenticator.ValidateMultiFactorCodeAsync(body)
            .ThrowsAsync(new InvalidValidationCodeException("bad"));

        var result = await VerifyValidationCodeEndpoint.HandleAsync(body, authenticator);

        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(400);
    }
}
