using Cause.SecurityManagement.Controllers;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Controllers;

[TestFixture]
public class ExternalSystemAuthenticationControllerTests
{
    private ExternalSystemAuthenticationController controller;    
    private IExternalSystemAuthenticationService externalSystemAuthenticationService;
    private readonly ExternalSystemLoginInformations externalSystemLoginInformations = new()
    {
        Apikey = "anApiKey"
    };

    [SetUp]
    public void Setup()
    {
        externalSystemAuthenticationService = Substitute.For<IExternalSystemAuthenticationService>();
        controller = new ExternalSystemAuthenticationController(externalSystemAuthenticationService, Substitute.For<ILogger<ExternalSystemAuthenticationController>>());
    }

    [Test]
    public void WithoutLoginInformations_WhenLogonForExternalSystem_ShouldReturnUnauthorize()
    {
        var result = controller.Logon(null);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        result.Value.Should().Be(null);
    }

    [Test]
    public void WithInvalidLoginInformations_WhenLogonForExternalSystem_ShouldReturnUnauthorize()
    {
        var result = controller.Logon(externalSystemLoginInformations);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        result.Value.Should().Be(null);
    }

    [Test]
    public void WithoutHeaderAuthorization_WhenLogonForExternalSystem_ShouldBeAccepted()
    {
        SetupValidExternalSystemLogin();

        var result = controller.Logon(externalSystemLoginInformations);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().Be(null);
        result.Value.Should().BeOfType<LoginResult>();
    }

    [Test]
    public void WithValidHeaderAuthorization_WhenLogonForExternalSystem_ShouldBeAccepted()
    {
        SetAuthorizationHeader("Bearer aToken");
        SetupValidExternalSystemLogin();

        var result = controller.Logon(externalSystemLoginInformations);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().Be(null);
        result.Value.Should().BeOfType<LoginResult>();
    }

    [Test]
    public void WithInvalidHeaderAuthorization_WhenLogonForExternalSystem_ShouldBeAccepted()
    {
        SetAuthorizationHeader("aToken");
        SetupValidExternalSystemLogin();

        var result = controller.Logon(externalSystemLoginInformations);

        result.Should().BeOfType<ActionResult<LoginResult>>();
        result.Result.Should().Be(null);
        result.Value.Should().BeOfType<LoginResult>();
    }

    private void SetupValidExternalSystemLogin()
    {
        var aToken = new ExternalSystemToken();
        var aExternalSystem = new ExternalSystem();

        externalSystemAuthenticationService.Login(Arg.Any<string>()).Returns((aToken, aExternalSystem));
    }

    private void SetAuthorizationHeader(string value)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = value;
    }
}