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
using Cause.SecurityManagement.Models.ValidationCode;

namespace Cause.SecurityManagement.Tests.Services;

[TestFixture]
public class UserAuthenticatorTests
{
    private IUserRepository<User> repository;
    private IUserPermissionService managementService;
    private ITokenGenerator generator;
    private ICurrentUserService currentUserService;
    private IAuthenticationMultiFactorHandler<User> multiAuthHandler;
    private IUserTokenGenerator userTokenGenerator;
    private UserAuthenticator<User> service;
    private SecurityManagementOptions options;
    private SecurityConfiguration configuration;
    private readonly Guid someUserId = Guid.NewGuid();

    [SetUp]
    public void SetUpTest()
    {
        configuration = new SecurityConfiguration { PackageName = "Fred's Unit Tests", Issuer = "Fred's issuer"};
        multiAuthHandler = Substitute.For<IAuthenticationMultiFactorHandler<User>>();
        currentUserService = Substitute.For<ICurrentUserService>();           
        repository = Substitute.For<IUserRepository<User>>();
        managementService = Substitute.For<IUserPermissionService>();
        generator = Substitute.For<ITokenGenerator>();
        userTokenGenerator = Substitute.For<IUserTokenGenerator>();
        service = new UserAuthenticator<User>(currentUserService, repository, managementService, multiAuthHandler, generator, userTokenGenerator, Options.Create(configuration));
        options = new SecurityManagementOptions();
    }

    [Test]
    public async Task SomeUnknownUser_WhenLoggingIn_ShouldNotBeSuccessful()
    {
        var someUserName = "asdlkfj";
        var somePassword = "aclkvjb";
        repository.GetEntityWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);
        repository.GetEntity(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);

        var result = await service.LoginAsync(someUserName, somePassword);

        result.Should().BeNull();
        repository.DidNotReceive().AddToken(Arg.Any<UserToken>());
    }

    [Test]
    public async Task SomeRecognizedUserWithTemporaryPassword_WhenLoggingIn_ShouldReturnCredentialsWithPasswordSetupRole()
    {
        var someUserName = "asdlkfj";
        var somePassword = "aclkvjb";
        var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = true };
        var expectedUserToken = new UserToken { AccessToken = "asldkfj", RefreshToken = "alskdjf", IdUser = someUser.Id, ForIssuer = configuration.Issuer};
        userTokenGenerator.GenerateEntityTokenAsync(Arg.Is(someUser), Arg.Is(SecurityRoles.UserPasswordSetup)).Returns(expectedUserToken);
        repository.GetEntityWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns(someUser);

        var result = await service.LoginAsync(someUserName, somePassword);

        await userTokenGenerator.Received(1).GenerateEntityTokenAsync(Arg.Is(someUser), Arg.Is(SecurityRoles.UserPasswordSetup));
        result.MustChangePassword.Should().BeTrue();
        result.AccessToken.Should().Be(expectedUserToken.AccessToken);
        result.RefreshToken.Should().Be(expectedUserToken.RefreshToken);
    }

    [Test]
    public async Task SomeRecognizedUser_WhenLoggingIn_ShouldReturnCredentialsWithRegularRole()
    {
        var someUserName = "asdlkfj";
        var somePassword = "aclkvjb";
        var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = false };
        var expectedUserToken = new UserToken { AccessToken = "asldkfj", RefreshToken = "alskdjf", IdUser = someUser.Id, ForIssuer = configuration.Issuer };
        repository.GetEntityWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);
        repository.GetEntity(Arg.Is(someUserName), Arg.Any<string>()).Returns(someUser);
        userTokenGenerator.GenerateEntityTokenAsync(Arg.Is(someUser), Arg.Is(SecurityRoles.User)).Returns(expectedUserToken);

        var result = await service.LoginAsync(someUserName, somePassword);

        await userTokenGenerator.Received(1).GenerateEntityTokenAsync(Arg.Is(someUser), Arg.Is(SecurityRoles.User));
        result.MustChangePassword.Should().BeFalse();
        result.AccessToken.Should().Be(expectedUserToken.AccessToken);
        result.RefreshToken.Should().Be(expectedUserToken.RefreshToken);
    }

    [Test]
    public async Task MultiFactorActivated_WhenLoggingIn_ShouldReturnCredentialsWithMultiAuthRole()
    {
        options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();
        var someUserName = "asdlkfj";
        var somePassword = "aclkvjb";
        var someUser = new User { UserName = "asdf", PasswordMustBeResetAfterLogin = false };
        var expectedUserToken = new UserToken();
        repository.GetEntityWithTemporaryPassword(Arg.Is(someUserName), Arg.Is(somePassword)).Returns((User)null);
        repository.GetEntity(Arg.Is(someUserName), Arg.Any<string>()).Returns(someUser);
        userTokenGenerator.GenerateEntityTokenAsync(Arg.Is(someUser), Arg.Is(SecurityRoles.UserLoginWithMultiFactor)).Returns(expectedUserToken);

        var result = await service.LoginAsync(someUserName, somePassword);

        result.AccessToken.Should().Be(expectedUserToken.AccessToken);
        result.RefreshToken.Should().Be(expectedUserToken.RefreshToken);
        result.MustChangePassword.Should().BeFalse();
        await userTokenGenerator.Received(1).GenerateEntityTokenAsync(Arg.Is(someUser), Arg.Is(SecurityRoles.UserLoginWithMultiFactor));
    }

    [Test]
    public async Task SomeUser_WhenRequestingNewActivationCode_ShouldAskHandlerToSendIt()
    {
        var someUser = new User();
        currentUserService.GetUserId().Returns(someUserId);
        repository.GetEntityById(Arg.Is(someUserId)).Returns(someUser);

        await service.SendNewCodeAsync();

        await multiAuthHandler.Received(1).SendNewValidationCodeAsync(Arg.Is(someUser));
    }

    [Test]
    public async Task SomeUserWithoutSpecificCommunicationType_WhenRequestingNewActivationCode_ShouldAskHandlerToSendItBySms()
    {
        var someUser = new User();
        currentUserService.GetUserId().Returns(someUserId);
        repository.GetEntityById(Arg.Is(someUserId)).Returns(someUser);

        await service.SendNewCodeAsync();

        await multiAuthHandler.Received(1).SendNewValidationCodeAsync(Arg.Is(someUser), Arg.Is(ValidationCodeCommunicationType.Sms));
    }

    [Test]
    public async Task SomeUserWithVoiceCommunicationType_WhenRequestingNewActivationCode_ShouldAskHandlerToSendItByVoice()
    {
        var someUser = new User();
        currentUserService.GetUserId().Returns(someUserId);
        repository.GetEntityById(Arg.Is(someUserId)).Returns(someUser);

        await service.SendNewCodeAsync(ValidationCodeCommunicationType.Voice);

        await multiAuthHandler.Received(1).SendNewValidationCodeAsync(Arg.Is(someUser), Arg.Is(ValidationCodeCommunicationType.Voice));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SomeUser_WhenVerifyingAuthenticationState_ShouldCheckInDatabaseIfTokenIsStillValid(bool isLoggedIn)
    {
        var someToken = "hey ho!";
        currentUserService.GetUserId().Returns(someUserId);
        repository.HasToken(Arg.Is(someUserId), Arg.Is(someToken)).Returns(isLoggedIn);

        var result = service.IsLoggedIn(someToken);

        result.Should().Be(isLoggedIn);
        repository.Received(1).HasToken(Arg.Is(someUserId), Arg.Is(someToken));
    }
}