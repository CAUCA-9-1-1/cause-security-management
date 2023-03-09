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
            service = new AuthenticationService<User>(currentUserService, repository, managementService, multiAuthHandler, reader, generator, Options.Create(configuration));
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
            var someRefreshToken = "asdfa";
            var someAccessToken = "lkjlkj";
            var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = true };
            repository.GetUserWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns(someUser);
            generator.GenerateAccessToken(Arg.Is(someUser.Id.ToString()), Arg.Is(someUser.UserName), Arg.Is(SecurityRoles.UserPasswordSetup)).Returns(someAccessToken);
            generator.GenerateRefreshToken().Returns(someRefreshToken);

            var (_, userFound) = await service.LoginAsync(someUserName, somePassword);

            userFound.PasswordMustBeResetAfterLogin.Should().Be(true);
            generator.Received(1).GenerateAccessToken(Arg.Is(someUser.Id.ToString()), Arg.Is(someUser.UserName), Arg.Is(SecurityRoles.UserPasswordSetup));
        }

        [Test]
        public async Task SomeRecognizedUser_WhenLoggingIn_ShouldReturnCredentialsWithRegularRole()
        {
            var someUserName = "asdlkfj";
            var somePassword = "aclkvjb";
            var someRefreshToken = "asdfa";
            var someAccessToken = "lkjlkj";
            var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = false };
            repository.GetUserWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);
            repository.GetUser(Arg.Is(someUserName), Arg.Any<string>()).Returns(someUser);
            generator.GenerateAccessToken(Arg.Is(someUser.Id.ToString()), Arg.Is(someUser.UserName), Arg.Is(SecurityRoles.User)).Returns(someAccessToken);
            generator.GenerateRefreshToken().Returns(someRefreshToken);

            var (_, userFound) = await service.LoginAsync(someUserName, somePassword);

            userFound.PasswordMustBeResetAfterLogin.Should().Be(false);
            generator.Received(1).GenerateAccessToken(Arg.Is(someUser.Id.ToString()), Arg.Is(someUser.UserName), Arg.Is(SecurityRoles.User));
        }

        [Test]
        public async Task MultiFactorActivated_WhenLoggingIn_ShouldReturnCredentialsWithMultiAuthRole()
        {
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();
            var someUserName = "asdlkfj";
            var somePassword = "aclkvjb";
            var someRefreshToken = "asdfa";
            var someAccessToken = "lkjlkj";
            var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = false };
            repository.GetUserWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);
            repository.GetUser(Arg.Is(someUserName), Arg.Any<string>()).Returns(someUser);
            generator.GenerateAccessToken(Arg.Is(someUser.Id.ToString()), Arg.Is(someUser.UserName), Arg.Is(SecurityRoles.User)).Returns(someAccessToken);
            generator.GenerateRefreshToken().Returns(someRefreshToken);

            var (_, userFound) = await service.LoginAsync(someUserName, somePassword);

            userFound.PasswordMustBeResetAfterLogin.Should().Be(false);
            generator.Received(1).GenerateAccessToken(Arg.Is(someUser.Id.ToString()), Arg.Is(someUser.UserName), Arg.Is(SecurityRoles.UserLoginWithMultiFactor));
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