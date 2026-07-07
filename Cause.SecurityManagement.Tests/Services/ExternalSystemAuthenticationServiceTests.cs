using System;
using System.Threading.Tasks;

using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;

using Cause.SecurityManagement.Core.Repositories;

using Cause.SecurityManagement.Core.Services;

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
            var someExternalSystem = new ExternalSystem { Name = "asdf", AuthenticationType = ExternalSystemAuthenticationType.Token };
            repository.GetByApiKey(Arg.Is(someKnownApikey)).Returns(someExternalSystem);
            generator.GenerateAccessToken(Arg.Is(someExternalSystem.Id.ToString()), Arg.Is(someExternalSystem.Name), Arg.Is(SecurityRoles.ExternalSystem), Arg.Any<CustomClaims>()).Returns(someAccessToken);
            generator.GenerateRefreshToken().Returns(someRefreshToken);

            var (token, system) = service.Login(someKnownApikey);

            token.Should().NotBeNull();
            token.AccessToken.Should().Be(someAccessToken);
            token.RefreshToken.Should().Be(someRefreshToken);
            token.IdExternalSystem.Should().Be(someExternalSystem.Id);
            system.Should().Be(someExternalSystem);
            repository.Received(1).AddToken(Arg.Is<ExternalSystemToken>(systemToken => systemToken.IdExternalSystem == someExternalSystem.Id && systemToken.AccessToken == someAccessToken && systemToken.RefreshToken == someRefreshToken));
        }

        [Test]
        public void SomeTokenBasedApi_WhenLoggingIn_ShouldIncludeAuthenticationTypeClaim()
        {
            var someKnownApikey = "asdlkfj";
            var someExternalSystem = new ExternalSystem { Name = "asdf", AuthenticationType = ExternalSystemAuthenticationType.Token };
            repository.GetByApiKey(Arg.Is(someKnownApikey)).Returns(someExternalSystem);

            service.Login(someKnownApikey);

            generator.Received(1).GenerateAccessToken(
                Arg.Is(someExternalSystem.Id.ToString()),
                Arg.Is(someExternalSystem.Name),
                Arg.Is(SecurityRoles.ExternalSystem),
                Arg.Is<CustomClaims>(claim => claim.Type == ExternalSystemClaims.AuthenticationType && claim.Value == nameof(ExternalSystemAuthenticationType.Token)));
        }

        [Test]
        public async Task SomeCertificateBasedApi_WhenRefreshingToken_ShouldIncludeAuthenticationTypeClaim()
        {
            var someExternalSystemId = Guid.NewGuid();
            var someToken = "expired-token";
            var someRefreshToken = "some-refresh-token";
            var someExternalSystem = new ExternalSystem { Id = someExternalSystemId, Name = "asdf", AuthenticationType = ExternalSystemAuthenticationType.Certificate };
            var someExternalSystemToken = new ExternalSystemToken { IdExternalSystem = someExternalSystemId };
            reader.GetSidFromExpiredToken(Arg.Is(someToken)).Returns(someExternalSystemId.ToString());
            repository.GetCurrentToken(Arg.Is(someExternalSystemId), Arg.Is(someRefreshToken)).Returns(someExternalSystemToken);
            repository.GetById(Arg.Is(someExternalSystemId)).Returns(someExternalSystem);

            await service.RefreshAccessTokenAsync(someToken, someRefreshToken);

            generator.Received(1).GenerateAccessToken(
                Arg.Is(someExternalSystem.Id.ToString()),
                Arg.Is(someExternalSystem.Name),
                Arg.Is(SecurityRoles.ExternalSystem),
                Arg.Is<CustomClaims>(claim => claim.Type == ExternalSystemClaims.AuthenticationType && claim.Value == nameof(ExternalSystemAuthenticationType.Certificate)));
        }
    }
}