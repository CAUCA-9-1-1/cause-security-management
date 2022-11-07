using System;
using System.Text;
using System.Text.Json;
using Cause.SecurityManagement.Controllers;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Tests.Controllers
{
    [TestFixture]
    public class AuthenticationControllerTests
    {
        private IUserPermissionRepository permissionsReader;
        private IAuthenticationService authenticationService;
        private IExternalSystemAuthenticationService externalSystemAuthenticationService;
        private IMobileVersionService mobileVersionService;
        private ILogger<AuthenticationController> logger;
        private AuthenticationController controller;

        private LoginInformations loginInformations = new LoginInformations
        {
            UserName = "aUserName",
            Password = "aPassword",
        };
        private ExternalSystemLoginInformations externalSystemLoginInformations = new ExternalSystemLoginInformations
        {
            Apikey = "anApiKey"
        };

        [SetUp]
        public void SetUpTest()
        {
            permissionsReader = Substitute.For<IUserPermissionRepository>();
            authenticationService = Substitute.For<IAuthenticationService>();
            externalSystemAuthenticationService = Substitute.For<IExternalSystemAuthenticationService>();
            mobileVersionService = Substitute.For<IMobileVersionService>();
            logger = Substitute.For<ILogger<AuthenticationController>>();
            controller = new AuthenticationController(permissionsReader, authenticationService, externalSystemAuthenticationService, mobileVersionService, logger);
        }

        [Test]
        public async Task SomeUser_WhenRequestingNewValidationCode_ShouldAskAuthenticationServiceToSendCode()
        {
            var result = await controller.SendNewCodeAsync();

            result.Should().BeOfType<OkResult>();
            await authenticationService.Received(1).SendNewCodeAsync();
        }

        [Test]
        public async Task UserWithoutKnownValidationCode_WhenRequestingNewValidationCode_ShouldReturnError()
        {
            authenticationService.When(mock => mock.SendNewCodeAsync()).Do((_) => throw new UserValidationCodeNotFoundException());

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

            await authenticationService.Received(1).LoginAsync(Arg.Is(loginInformations.UserName), Arg.Is(loginInformations.Password));
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

            await authenticationService.Received(1).LoginAsync(Arg.Is(loginInformations.UserName), Arg.Is(loginInformations.Password));
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
        public void WithoutLoginInformations_WhenLogonForExternalSystem_ShouldReturnUnauthorize()
        {
            var result = controller.LogonForExternalSystem(null);

            result.Should().BeOfType<ActionResult<LoginResult>>();
            result.Result.Should().BeOfType<UnauthorizedResult>();
            result.Value.Should().Be(null);
        }

        [Test]
        public void WithInvalidLoginInformations_WhenLogonForExternalSystem_ShouldReturnUnauthorize()
        {
            var result = controller.LogonForExternalSystem(externalSystemLoginInformations);

            result.Should().BeOfType<ActionResult<LoginResult>>();
            result.Result.Should().BeOfType<UnauthorizedResult>();
            result.Value.Should().Be(null);
        }

        [Test]
        public void WithoutHeaderAuthorization_WhenLogonForExternalSystem_ShouldBeAccepted()
        {
            SetupValidExternalSystemLogin();

            var result = controller.LogonForExternalSystem(externalSystemLoginInformations);

            result.Should().BeOfType<ActionResult<LoginResult>>();
            result.Result.Should().Be(null);
            result.Value.Should().BeOfType<LoginResult>();
        }

        [Test]
        public void WithValidHeaderAuthorization_WhenLogonForExternalSystem_ShouldBeAccepted()
        {
            SetAuthorizationHeader("Bearer aToken");
            SetupValidExternalSystemLogin();

            var result = controller.LogonForExternalSystem(externalSystemLoginInformations);

            result.Should().BeOfType<ActionResult<LoginResult>>();
            result.Result.Should().Be(null);
            result.Value.Should().BeOfType<LoginResult>();
        }

        [Test]
        public void WithInvalidHeaderAuthorization_WhenLogonForExternalSystem_ShouldBeAccepted()
        {
            SetAuthorizationHeader("aToken");
            SetupValidExternalSystemLogin();

            var result = controller.LogonForExternalSystem(externalSystemLoginInformations);

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

        private void SetupValidUserLogin()
        {
            var aUserToken = new UserToken();
            var aUser = new User();

            authenticationService.LoginAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((aUserToken, aUser));
        }

        private void SetAuthorizationHeader(string value)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = value;
        }
    }
}
