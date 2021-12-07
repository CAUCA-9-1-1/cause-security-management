using Cause.SecurityManagement.Controllers;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Tests.Controllers
{
    [TestFixture]
    public class AuthenticationControllerTests
    {
        private IUserPermissionRepository permissionsReader;
        private IAuthenticationService service;
        private IExternalSystemAuthenticationService externalSystemAuthenticationService;
        private IMobileVersionService mobileVersionService;
        private AuthenticationController controller;        

        [SetUp]
        public void SetUpTest()
        {
            permissionsReader = Substitute.For<IUserPermissionRepository>();
            service = Substitute.For<IAuthenticationService>();
            externalSystemAuthenticationService = Substitute.For<IExternalSystemAuthenticationService>();
            mobileVersionService = Substitute.For<IMobileVersionService>();
            controller = new AuthenticationController(permissionsReader, service, externalSystemAuthenticationService, mobileVersionService);
        }

        [Test]
        public async Task SomeUser_WhenRequestingNewValidationCode_ShouldAskAuthenticationServiceToSendCode()
        {
            var result = await controller.SendNewCodeAsync();

            result.Should().BeOfType<OkResult>();
            await service.Received(1).SendNewCodeAsync();
        }

        [Test]
        public async Task UserWithoutKnownValidationCode_WhenRequestingNewValidationCode_ShouldReturnError()
        {
            service.When(mock => mock.SendNewCodeAsync()).Do((_) => throw new UserValidationCodeNotFoundException());

            var result = await controller.SendNewCodeAsync();

            result.Should().BeOfType<UnauthorizedResult>();
        }
    }
}
