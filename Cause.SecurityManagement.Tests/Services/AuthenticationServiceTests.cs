using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Tests.Services
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        private IUserRepository<User> repository;
        private IUserManagementService<User> managementService;
        private ITokenReader reader;
        private ITokenGenerator generator;
        private ICurrentUserService currentUserService;
        private IAuthenticationMultiFactorHandler<User> multiAuthHandler;
        private IUserTokenGenerator userTokenGenerator;
        private AuthenticationService<User> service;
        private SecurityManagementOptions options;
        private SecurityConfiguration configuration;
        private readonly Guid someUserId = Guid.NewGuid();

        [SetUp]
        public void SetUpTest()
        {
            configuration = new SecurityConfiguration { PackageName = "Fred's Unit Tests" };
            multiAuthHandler = Substitute.For<IAuthenticationMultiFactorHandler<User>>();
            currentUserService = Substitute.For<ICurrentUserService>();           
            repository = Substitute.For<IUserRepository<User>>();
            managementService = Substitute.For<IUserManagementService<User>>();
            reader = Substitute.For<ITokenReader>();
            generator = Substitute.For<ITokenGenerator>();
            userTokenGenerator = Substitute.For<IUserTokenGenerator>();
            service = new AuthenticationService<User>(currentUserService, repository, managementService, multiAuthHandler, reader, generator, userTokenGenerator, Options.Create(configuration));
            options = new SecurityManagementOptions();
        }

        [Test]
        public async Task SomeUnknownUser_WhenLoggingIn_ShouldNotBeSuccessful()
        {
            var someUserName = "asdlkfj";
            var somePassword = "aclkvjb";
            repository.GetUserWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);
            repository.GetUser(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);

            var (token, system) = await service.LoginAsync(someUserName, somePassword);

            token.Should().BeNull();
            system.Should().BeNull();
            repository.DidNotReceive().AddToken(Arg.Any<UserToken>());
        }

        [Test]
        public async Task SomeRecognizedUserWithTemporaryPassword_WhenLoggingIn_ShouldReturnCredentialsWithPasswordSetupRole()
        {
            var someUserName = "asdlkfj";
            var somePassword = "aclkvjb";
            var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = true };
            var expectedUserToken = new UserToken { AccessToken = "asldkfj", RefreshToken = "alskdjf", IdUser = someUser.Id };
            userTokenGenerator.GenerateUserToken(Arg.Is(someUser), Arg.Is(SecurityRoles.UserPasswordSetup)).Returns(expectedUserToken);
            repository.GetUserWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns(someUser);

            var (userTokenGenerated, userFound) = await service.LoginAsync(someUserName, somePassword);

            userTokenGenerator.Received(1).GenerateUserToken(Arg.Is(someUser), Arg.Is(SecurityRoles.UserPasswordSetup));
            userFound.PasswordMustBeResetAfterLogin.Should().Be(true);
            userTokenGenerated.Should().Be(expectedUserToken);
        }

        [Test]
        public async Task SomeRecognizedUser_WhenLoggingIn_ShouldReturnCredentialsWithRegularRole()
        {
            var someUserName = "asdlkfj";
            var somePassword = "aclkvjb";
            var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = false };
            var expectedUserToken = new UserToken { AccessToken = "asldkfj", RefreshToken = "alskdjf", IdUser = someUser.Id };
            repository.GetUserWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);
            repository.GetUser(Arg.Is(someUserName), Arg.Any<string>()).Returns(someUser);
            userTokenGenerator.GenerateUserToken(Arg.Is(someUser), Arg.Is(SecurityRoles.User)).Returns(expectedUserToken);

            var (userTokenGenerated, userFound) = await service.LoginAsync(someUserName, somePassword);

            userTokenGenerator.Received(1).GenerateUserToken(Arg.Is(someUser), Arg.Is(SecurityRoles.User));
            userFound.PasswordMustBeResetAfterLogin.Should().Be(false);
            userTokenGenerated.Should().Be(expectedUserToken);
        }

        [Test]
        public async Task MultiFactorActivated_WhenLoggingIn_ShouldReturnCredentialsWithMultiAuthRole()
        {
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();
            var someUserName = "asdlkfj";
            var somePassword = "aclkvjb";
            var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = false };
            var expectedUserToken = new UserToken();
            repository.GetUserWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);
            repository.GetUser(Arg.Is(someUserName), Arg.Any<string>()).Returns(someUser);
            userTokenGenerator.GenerateUserToken(Arg.Is(someUser), Arg.Is(SecurityRoles.UserLoginWithMultiFactor)).Returns(expectedUserToken);

            var (userTokenGenerated, userFound) = await service.LoginAsync(someUserName, somePassword);

            userTokenGenerated.Should().Be(expectedUserToken);
            userFound.PasswordMustBeResetAfterLogin.Should().Be(false);
            userTokenGenerator.Received(1).GenerateUserToken(Arg.Is(someUser), Arg.Is(SecurityRoles.UserLoginWithMultiFactor));
        }

        [Test]
        public async Task SomeUser_WhenRequestingNewActivationCode_ShouldAskHandlerToSendIt()
        {
            var someUser = new User();
            currentUserService.GetUserId().Returns(someUserId);
            repository.GetUserById(Arg.Is(someUserId)).Returns(someUser);

            await service.SendNewCodeAsync();

            await multiAuthHandler.Received(1).SendNewValidationCodeAsync(Arg.Is(someUser));
        }
    }
}