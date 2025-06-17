using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services
{
    [TestFixture]
    public class ExternalSystemAuthenticationServiceTests
    {
        private IExternalSystemRepository repository;
        private ITokenReader reader;
        private ITokenGenerator generator;
        private ExternalSystemAuthenticationService service;

        [SetUp]
        public void SetUpTest()
        {
            repository = Substitute.For<IExternalSystemRepository>();
            reader = Substitute.For<ITokenReader>();
            generator = Substitute.For<ITokenGenerator>();
            service = new ExternalSystemAuthenticationService(repository, reader, generator);
        }

        [Test]
        public void SomeUnknownApi_WhenLoggingIn_ShouldNotBeSuccessful()
        {
            var someUnknownApikey = "asdlkfj";
            repository.GetByApiKey(Arg.Is(someUnknownApikey)).Returns((ExternalSystem)null);

            var (token, system) = service.Login(someUnknownApikey);

            token.Should().BeNull();
            system.Should().BeNull();
            repository.DidNotReceive().AddToken(Arg.Any<ExternalSystemToken>());
        }

        [Test]
        public void SomeRecognizedApi_WhenLoggingIn_ShouldReturnCredentials()
        {
            var someRefreshToken = "asdfa";
            var someAccessToken = "lkjlkj";
            var someKnownApikey = "asdlkfj";
            var someExternalSystem = new ExternalSystem { Name = "asdf" };
            repository.GetByApiKey(Arg.Is(someKnownApikey)).Returns(someExternalSystem);
            generator.GenerateAccessToken(Arg.Is(someExternalSystem.Id.ToString()), Arg.Is(someExternalSystem.Name), Arg.Is(SecurityRoles.ExternalSystem)).Returns(someAccessToken);
            generator.GenerateRefreshToken().Returns(someRefreshToken);

            var (token, system) = service.Login(someKnownApikey);

            token.Should().NotBeNull();
            token.AccessToken.Should().Be(someAccessToken);
            token.RefreshToken.Should().Be(someRefreshToken);
            token.IdExternalSystem.Should().Be(someExternalSystem.Id);
            system.Should().Be(someExternalSystem);
            repository.Received(1).AddToken(Arg.Is<ExternalSystemToken>(systemToken => systemToken.IdExternalSystem == someExternalSystem.Id && systemToken.AccessToken == someAccessToken && systemToken.RefreshToken == someRefreshToken));
        }
    }
}