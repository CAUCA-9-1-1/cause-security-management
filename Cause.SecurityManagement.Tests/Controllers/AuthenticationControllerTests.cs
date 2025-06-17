using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Cause.SecurityManagement.Controllers;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models.ValidationCode;
using NSubstitute.ExceptionExtensions;
using Microsoft.IdentityModel.Tokens;

namespace Cause.SecurityManagement.Tests.Controllers;

[TestFixture]
public class AuthenticationControllerTests
{
    private ICurrentUserService currentUserService;
    private IUserAuthenticator userAuthenticator;
    private IUserTokenRefresher userTokenRefresher;
    private IMobileVersionValidator mobileVersionValidator;
    private ILogger<AuthenticationController> logger;
    private AuthenticationController controller;

    private readonly LoginInformations loginInformations = new()
    {
        UserName = "aUserName",
        Password = "aPassword",
    };

    [SetUp]
    public void SetUpTest()
    {
        currentUserService = Substitute.For<ICurrentUserService>();
        userAuthenticator = Substitute.For<IUserAuthenticator>();
        userTokenRefresher = Substitute.For<IUserTokenRefresher>();
        mobileVersionValidator = Substitute.For<IMobileVersionValidator>();
        logger = Substitute.For<ILogger<AuthenticationController>>();
        controller = new AuthenticationController(currentUserService, userAuthenticator, userTokenRefresher, mobileVersionValidator, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Test]
    public async Task SomeUser_WhenRequestingNewValidationCode_ShouldAskAuthenticationServiceToSendCode()
    {
        var result = await controller.SendNewCodeAsync();

        result.Should().BeOfType<OkResult>();
        await userAuthenticator.Received(1).SendNewCodeAsync(Arg.Is(ValidationCodeCommunicationType.Sms));
    }

    [Test]
    public async Task SomeUserWithVoiceCommunicationType_WhenRequestingNewValidationCode_ShouldAskAuthenticationServiceToSendCode()
    {
        var result = await controller.SendNewCodeAsync(ValidationCodeCommunicationType.Voice);

        result.Should().BeOfType<OkResult>();
        await userAuthenticator.Received(1).SendNewCodeAsync(Arg.Is(ValidationCodeCommunicationType.Voice));
    }

    [Test]
    public async Task UserWithoutKnownValidationCode_WhenRequestingNewValidationCode_ShouldReturnError()
    {
        userAuthenticator.When(mock => mock.SendNewCodeAsync()).Do((_) => throw new UserValidationCodeNotFoundException());

        var result = await controller.SendNewCodeAsync();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Test]
    public async Task WithoutLoginInformation_WhenLogon_ShouldReturnUnauthorize()
    {
        var result = await controller.Logon(null, null);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        result.Value.Should().Be(null);
    }

    [Test]
    public async Task WithInvalidInformation_WhenLogon_ShouldReturnUnauthorize()
    {
        var result = await controller.Logon(null, loginInformations);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        result.Value.Should().Be(null);
    }

    [Test]
    public async Task WithoutHeaderAuthorization_WhenLogon_ShouldBeAccepted()
    {
        SetupValidUserLogin();

        var result = await controller.Logon(null, loginInformations);

        await userAuthenticator.Received(1).LoginAsync(Arg.Is(loginInformations.UserName), Arg.Is(loginInformations.Password));
        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().Be(null);
        result.Value.Should().BeOfType<LoginResult>();
    }

    [Test]
    public async Task WithoutHeaderAuthorization_WhenLogonThroughHeader_ShouldBeAccepted()
    {
        SetupValidUserLogin();
        var loginInformationHeader = Convert.ToBase64String(Encoding.Default.GetBytes(Uri.EscapeDataString(JsonSerializer.Serialize(loginInformations))));

        var result = await controller.Logon(loginInformationHeader, null);

        await userAuthenticator.Received(1).LoginAsync(Arg.Is(loginInformations.UserName), Arg.Is(loginInformations.Password));
        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().Be(null);
        result.Value.Should().BeOfType<LoginResult>();
    }

    [Test]
    public async Task WithValidHeaderAuthorization_WhenLogon_ShouldBeAccepted()
    {
        SetAuthorizationHeader("Bearer aToken");
        SetupValidUserLogin();

        var result = await controller.Logon(null, loginInformations);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().Be(null);
        result.Value.Should().BeOfType<LoginResult>();
    }

    [Test]
    public async Task WithInvalidHeaderAuthorization_WhenLogon_ShouldBeAccepted()
    {
        SetAuthorizationHeader("aToken");
        SetupValidUserLogin();

        var result = await controller.Logon(null, loginInformations);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().Be(null);
        result.Value.Should().BeOfType<LoginResult>();
    }

    [Test]
    public async Task WithValidInformation_WhenGettingNewAccessToken_ShouldReturnNewToken()
    {
        var requestData = new TokenRefreshResult { AccessToken = "oldToken", RefreshToken = "SomeRefreshToken" };
        var newAccessToken = "newToken";
        userTokenRefresher.GetNewAccessTokenAsync(Arg.Is(requestData.AccessToken), Arg.Is(requestData.RefreshToken)).Returns(newAccessToken);

        var result = await controller.GetNewAccessTokenAsync(requestData);

        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!.Value.Should().BeEquivalentTo(new { AccessToken = newAccessToken, requestData.RefreshToken });
    }

    [Test]
    public void UnauthorizedUser_WhenCheckingState_ShouldNotBeAuthorized()
    {
        var someRefreshToken = "twi108";
        userAuthenticator.IsLoggedIn(Arg.Is(someRefreshToken)).Returns(false);

        var result = controller.GetAuthenticationState(new() { RefreshToken = someRefreshToken });

        result.Should().BeFalse();
    }

    [Test]
    public void AuthorizedUser_WhenCheckingState_ShouldBeAuthorized()
    {
        var someRefreshToken = "twi108";
        userAuthenticator.IsLoggedIn(Arg.Is(someRefreshToken)).Returns(true);

        var result = controller.GetAuthenticationState(new() { RefreshToken = someRefreshToken });

        result.Should().BeTrue();
    }

    [Test]
    public void SomeUser_WhenChangingTemporaryPassword_ShouldUpdateIt()
    {
        var somePassword = "aPassword";
        var someUserId = Guid.NewGuid();
        var someRequest = new PasswordChangeRequest { NewPassword = somePassword };
        currentUserService.GetUserId().Returns(someUserId);

        var result = controller.UpdatePassword(someRequest);

        userAuthenticator.Received(1).ChangePassword(Arg.Is(somePassword));
        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task SomeUser_WhenRecoveringAccount_ShouldSendRecoveryEmail()
    {
        var someEmail = "anEmail";
        var someRequest = new AccountRecoveryRequest { Email = someEmail };

        var result = await controller.RecoverAccountAsync(someRequest);

        await userAuthenticator.Received(1).RecoverAccountAsync(Arg.Is(someEmail));
        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task SomeUser_WhenValidatingRecoveryAccountWithValidCode_ShouldReturnLoginResult()
    {
        var someEmail = "anEmail";
        var someValidationCode = "aCode";
        var someRequest = new AccountRecoveryValidationRequest { Email = someEmail, ValidationCode = someValidationCode };
        var someLoginResult = new LoginResult();
        userAuthenticator.ValidateAccountRecoveryAsync(Arg.Is(someEmail), Arg.Is(someValidationCode)).Returns(someLoginResult);

        var result = await controller.ValidateRecoverAccount(someRequest);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(someLoginResult);
    }

    [Test]
    public async Task SomeUser_WhenValidatingRecoveryAccountWithInvalidCode_ShouldReturnBadRequest()
    {
        var someEmail = "anEmail";
        var someValidationCode = "aCode";
        var someRequest = new AccountRecoveryValidationRequest { Email = someEmail, ValidationCode = someValidationCode };
        userAuthenticator.ValidateAccountRecoveryAsync(Arg.Is(someEmail), Arg.Is(someValidationCode)).Returns((LoginResult)null);

        var result = await controller.ValidateRecoverAccount(someRequest);

        result.Should().BeOfType<BadRequestResult>();
    }

    public static IEnumerable<TestCaseData> PossibleRefreshingExceptions
    {
        get
        {
            yield return new TestCaseData(new InvalidTokenException("bla", new Exception()), "Token-Invalid", "true");
            yield return new TestCaseData(new SecurityTokenExpiredException("bla", new Exception()), "Refresh-Token-Expired", "true");
            yield return new TestCaseData(new SecurityTokenException("bla", new Exception()), "Token-Invalid", "true");
            yield return new TestCaseData(new InvalidTokenUserException("bla", "blo", "bli"), "Token-Invalid", "true");
        }
    }

    [TestCaseSource(nameof(PossibleRefreshingExceptions))]
    public async Task WithInvalidInformation_WhenGettingNewAccessToken_ShouldBeUnauthorized(Exception exception, string expectedHeader, string expectedHeaderValue)
    {
        var requestData = new TokenRefreshResult { AccessToken = "oldToken", RefreshToken = "SomeRefreshToken" };
        userTokenRefresher.GetNewAccessTokenAsync(Arg.Is(requestData.AccessToken), Arg.Is(requestData.RefreshToken)).ThrowsAsync(exception);

        var result = await controller.GetNewAccessTokenAsync(requestData);

        result.Should().BeOfType<UnauthorizedResult>();
        controller.Response.Headers.TryGetValue(expectedHeader, out var resultHeaderValue);
        resultHeaderValue.Should().BeEquivalentTo(expectedHeaderValue);
    }

    private void SetupValidUserLogin()
    {
        var someLoginResult = new LoginResult();

        userAuthenticator.LoginAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(someLoginResult);
    }

    private void SetAuthorizationHeader(string value)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = value;
    }
}