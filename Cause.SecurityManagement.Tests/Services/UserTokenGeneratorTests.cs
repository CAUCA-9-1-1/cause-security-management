using System;
using System.Threading.Tasks;
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
        Generator = new UserTokenGenerator<User>(Options.Create(Configuration), TokenGenerator, Repository);
    }

    [TestCase(SecurityRoles.UserPasswordSetup)]
    [TestCase(SecurityRoles.UserCreation)]
    [TestCase(SecurityRoles.UserLoginWithMultiFactor)]
    [TestCase(SecurityRoles.UserRecovery)]
    public async Task SomeUser_WhenGeneratingTemporaryToken_ShouldNotGenerateDevice(string role)
    {
        TokenGenerator.GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(role)).Returns(ExpectedAccessToken);
        
        var result = await Generator.GenerateUserTokenAsync(SomeUser, role);

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
    public async Task SomeUser_WhenGeneratingTokenWithRegularRole_ShouldNotGenerateDevice()
    {
        TokenGenerator.GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(SecurityRoles.User)).Returns(ExpectedAccessToken);
        
        var result = await Generator.GenerateUserTokenAsync(SomeUser, SecurityRoles.User);

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
        Generator = new UserTokenGenerator<User>(Options.Create(Configuration), TokenGenerator, Repository, deviceManager);
    }

    [TestCase(SecurityRoles.UserPasswordSetup)]
    [TestCase(SecurityRoles.UserCreation)]
    [TestCase(SecurityRoles.UserLoginWithMultiFactor)]
    [TestCase(SecurityRoles.UserRecovery)]
    public async Task SomeUser_WhenGeneratingTemporaryToken_ShouldNotGenerateDevice(string role)
    {
        TokenGenerator.GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(role), Arg.Is<CustomClaims[]>(claims => claims.Length == 0)).Returns(ExpectedAccessToken);
        deviceManager.CreateNewDeviceAsync(Arg.Is(SomeUser.Id)).Returns(NewDeviceId);

        var result = await Generator.GenerateUserTokenAsync(SomeUser, role);

        result.IdUser.Should().Be(SomeUser.Id);
        result.SpecificDeviceId.Should().Be(null);
        result.ForIssuer.Should().Be(Configuration.Issuer);
        result.AccessToken.Should().Be(ExpectedAccessToken);
        result.RefreshToken.Should().BeEmpty();
        result.Role.Should().Be(role);
        Repository.Received(1).AddToken(Arg.Is(result));
        await deviceManager.DidNotReceive().CreateNewDeviceAsync(Arg.Any<Guid>());
        TokenGenerator.Received(1).GenerateRefreshTokenExpirationDate();
    }

    [Test]
    public async Task SomeUser_WhenGeneratingTokenWithRegularRole_ShouldGenerateDevice()
    {
        var expectedCustomClaims = new CustomClaims(AdditionalClaimsGenerator.DeviceIdType, NewDeviceId.ToString());
        TokenGenerator.GenerateAccessToken(Arg.Is(SomeUser.Id.ToString()), Arg.Is(SomeUser.UserName), Arg.Is(SecurityRoles.User), Arg.Is<CustomClaims>(claim => claim.Type == expectedCustomClaims.Type && claim.Value == expectedCustomClaims.Value)).Returns(ExpectedAccessToken);
        deviceManager.CreateNewDeviceAsync(Arg.Is(SomeUser.Id)).Returns(NewDeviceId);

        var result = await Generator.GenerateUserTokenAsync(SomeUser, SecurityRoles.User);

        result.IdUser.Should().Be(SomeUser.Id);
        result.SpecificDeviceId.Should().Be(NewDeviceId);
        result.ForIssuer.Should().Be(Configuration.Issuer);
        result.AccessToken.Should().Be(ExpectedAccessToken);
        result.RefreshToken.Should().Be(ExpectedRefreshToken);
        result.Role.Should().Be(SecurityRoles.User);
        Repository.Received(1).AddToken(Arg.Is(result));
        await deviceManager.Received(1).CreateNewDeviceAsync(Arg.Is(SomeUser.Id));
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
    protected UserTokenGenerator<User> Generator;
    protected IUserRepository<User> Repository;
    
    protected void GenerateSubstitutes()
    {
        Repository = Substitute.For<IUserRepository<User>>();
        TokenGenerator = Substitute.For<ITokenGenerator>();
        TokenGenerator.GenerateRefreshTokenExpirationDate().Returns(ExpectedExpirationDate);
        TokenGenerator.GenerateRefreshToken().Returns(ExpectedRefreshToken);
    }
}