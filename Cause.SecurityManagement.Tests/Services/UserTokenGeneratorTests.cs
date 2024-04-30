using System;
using System.Data;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services;

[TestFixture]
public class UserTokenGeneratorTestsWithoutDeviceManagement : BaseUserTokenGeneratorTest
{
    [SetUp]
    public void SetUpTest()
    {
        GenerateSubstitutes();
        Generator = new UserTokenGenerator(Options.Create(Configuration), TokenGenerator, Repository);
    }

    [TestCase(SecurityRoles.UserPasswordSetup)]
    [TestCase(SecurityRoles.UserCreation)]
    [TestCase(SecurityRoles.UserLoginWithMultiFactor)]
    [TestCase(SecurityRoles.UserRecovery)]
    public void SomeUser_WhenGeneratingTemporaryToken_ShouldNotGenerateDevice(string role)
    {
        TokenGenerator.GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(role)).Returns(ExpectedAccessToken);
        
        var result = Generator.GenerateUserToken(SomeUser, role);

        result.IdUser.Should().Be(SomeUser.Id);
        result.SpecificDeviceId.Should().Be(null);
        result.ForIssuer.Should().Be(Configuration.Issuer);
        result.AccessToken.Should().Be(ExpectedAccessToken);
        result.RefreshToken.Should().BeEmpty();
        result.Role.Should().Be(role);
        Repository.Received(1).AddToken(Arg.Is(result));
        TokenGenerator.Received(1).GenerateRefreshTokenExpirationDate();
        TokenGenerator.Received(1).GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(role));
    }

    [Test]
    public void SomeUser_WhenGeneratingTokenWithRegularRole_ShouldNotGenerateDevice()
    {
        TokenGenerator.GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(SecurityRoles.User)).Returns(ExpectedAccessToken);
        
        var result = Generator.GenerateUserToken(SomeUser, SecurityRoles.User);

        result.IdUser.Should().Be(SomeUser.Id);
        result.SpecificDeviceId.Should().Be(null);
        result.ForIssuer.Should().Be(Configuration.Issuer);
        result.AccessToken.Should().Be(ExpectedAccessToken);
        result.RefreshToken.Should().Be(ExpectedRefreshToken);
        result.Role.Should().Be(SecurityRoles.User);
        Repository.Received(1).AddToken(Arg.Is(result));
        TokenGenerator.Received(1).GenerateRefreshTokenExpirationDate();
        TokenGenerator.Received(1).GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(SecurityRoles.User));
    }
}

[TestFixture]
public class UserTokenGeneratorTestsWithDeviceManagement : BaseUserTokenGeneratorTest
{
    private IDeviceManager deviceManager;

    [SetUp]
    public void SetUpTest()
    {
        GenerateSubstitutes();
        deviceManager = Substitute.For<IDeviceManager>();
        Generator = new UserTokenGenerator(Options.Create(Configuration), TokenGenerator, Repository, deviceManager);
    }

    [TestCase(SecurityRoles.UserPasswordSetup)]
    [TestCase(SecurityRoles.UserCreation)]
    [TestCase(SecurityRoles.UserLoginWithMultiFactor)]
    [TestCase(SecurityRoles.UserRecovery)]
    public void SomeUser_WhenGeneratingTemporaryToken_ShouldNotGenerateDevice(string role)
    {
        TokenGenerator.GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(role)).Returns(ExpectedAccessToken);
        deviceManager.CreateNewDevice(Arg.Is(SomeUser.Id)).Returns(NewDeviceId);

        var result = Generator.GenerateUserToken(SomeUser, role);

        result.IdUser.Should().Be(SomeUser.Id);
        result.SpecificDeviceId.Should().Be(null);
        result.ForIssuer.Should().Be(Configuration.Issuer);
        result.AccessToken.Should().Be(ExpectedAccessToken);
        result.RefreshToken.Should().BeEmpty();
        result.Role.Should().Be(role);
        Repository.Received(1).AddToken(Arg.Is(result));
        deviceManager.DidNotReceive().CreateNewDevice(Arg.Any<Guid>());
        TokenGenerator.Received(1).GenerateRefreshTokenExpirationDate();
    }

    [Test]
    public void SomeUser_WhenGeneratingTokenWithRegularRole_ShouldGenerateDevice()
    {
        TokenGenerator.GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(SecurityRoles.User)).Returns(ExpectedAccessToken);
        deviceManager.CreateNewDevice(Arg.Is(SomeUser.Id)).Returns(NewDeviceId);

        var result = Generator.GenerateUserToken(SomeUser, SecurityRoles.User);

        result.IdUser.Should().Be(SomeUser.Id);
        result.SpecificDeviceId.Should().Be(NewDeviceId);
        result.ForIssuer.Should().Be(Configuration.Issuer);
        result.AccessToken.Should().Be(ExpectedAccessToken);
        result.RefreshToken.Should().Be(ExpectedRefreshToken);
        result.Role.Should().Be(SecurityRoles.User);
        Repository.Received(1).AddToken(Arg.Is(result));
        deviceManager.Received(1).CreateNewDevice(Arg.Is(SomeUser.Id));
        TokenGenerator.Received(1).GenerateRefreshTokenExpirationDate();
    }
}

public abstract class BaseUserTokenGeneratorTest
{
    protected readonly SecurityConfiguration Configuration = new() { Issuer = "Fred's Unit Tests" };
    protected readonly string ExpectedRefreshToken = "ExectedRefreshToken";
    protected readonly string ExpectedAccessToken = "ExpectedAccessToken";
    protected readonly User SomeUser = new() { IsActive = true, Id = Guid.NewGuid(), UserName = "SomeUserName"};
    protected readonly Guid NewDeviceId = Guid.NewGuid();
    protected readonly DateTime ExpectedExpirationDate = new(2099, 1, 1, 0, 0, 0, DateTimeKind.Local);

    protected ITokenGenerator TokenGenerator;
    protected UserTokenGenerator Generator;
    protected IUserRepository<User> Repository;
    
    protected void GenerateSubstitutes()
    {
        Repository = Substitute.For<IUserRepository<User>>();
        TokenGenerator = Substitute.For<ITokenGenerator>();
        TokenGenerator.GenerateRefreshTokenExpirationDate().Returns(ExpectedExpirationDate);
        TokenGenerator.GenerateRefreshToken().Returns(ExpectedRefreshToken);
    }
}