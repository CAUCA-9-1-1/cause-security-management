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
public class UserTokenRefresherWithoutDeviceManagerTests
{
    private readonly SecurityConfiguration configuration = new() { Issuer = "FCP" };

    private IUserRepository<User> repository;
    private ITokenGenerator tokenGenerator;
    private ITokenReader tokenReader;
    
    private UserTokenRefresher<User> refresher;


    [SetUp]
    public void SetUpTest()
    {
        repository = Substitute.For<IUserRepository<User>>();
        tokenGenerator = Substitute.For<ITokenGenerator>();
        tokenReader = Substitute.For<ITokenReader>();
        refresher = new UserTokenRefresher<User>(repository, tokenGenerator, tokenReader, Options.Create(configuration));
    }

    [Test]
    public async Task UserTokenWithoutIssuer_WhenRefreshing_ShouldSetIssuer()
    {
        var someAccessToken = "alsdkj";
        var someRefreshToken = "qweru";
        var someUserId = Guid.NewGuid();
        var someUserToken = new UserToken { ForIssuer = null, AccessToken = someAccessToken, RefreshToken = someRefreshToken, IdUser = someUserId };
        var someUser = new User { Id = someUserId, UserName = "Papouche128" };
        repository.GetEntityById(Arg.Is(someUserId)).Returns(someUser);
        repository.GetToken(Arg.Is(someUserId), Arg.Is(someRefreshToken)).Returns(someUserToken);
        tokenReader.GetSidFromExpiredToken(Arg.Is(someAccessToken)).Returns(someUserId.ToString());

        _ = await refresher.GetNewAccessTokenAsync(someAccessToken, someRefreshToken);

        someUserToken.ForIssuer.Should().Be(configuration.Issuer);
        tokenGenerator.Received(1).GenerateAccessToken(Arg.Is(someUserId.ToString()),
            Arg.Is(someUser.UserName),
            Arg.Is(SecurityRoles.User),
            Arg.Is<CustomClaims[]>(claims => claims.Length == 0));
    }
}

[TestFixture]
public class UserTokenRefresherWithDeviceManagerTests
{
    private readonly SecurityConfiguration configuration = new() { Issuer = "FCP" };

    private IUserRepository<User> repository;
    private ITokenGenerator tokenGenerator;
    private ITokenReader tokenReader;
    private IDeviceManager deviceManager;

    private UserTokenRefresher<User> refresher;


    [SetUp]
    public void SetUpTest()
    {
        repository = Substitute.For<IUserRepository<User>>();
        tokenGenerator = Substitute.For<ITokenGenerator>();
        tokenReader = Substitute.For<ITokenReader>();
        deviceManager = Substitute.For<IDeviceManager>();

        refresher = new UserTokenRefresher<User>(repository, tokenGenerator, tokenReader, Options.Create(configuration), deviceManager);
    }

    [Test]
    public async Task SomeUser_WhenRefreshing_ShouldGenerateTokenWithDeviceIdClaim()
    {
        var someAccessToken = "alsdkj";
        var someRefreshToken = "qweru";
        var someUserId = Guid.NewGuid();
        var someDeviceId = Guid.NewGuid();
        var someUserToken = new UserToken { ForIssuer = null, AccessToken = someAccessToken, RefreshToken = someRefreshToken, IdUser = someUserId, SpecificDeviceId = someDeviceId };
        var someUser = new User { Id = someUserId, UserName = "Papouche128" };

        repository.GetEntityById(Arg.Is(someUserId)).Returns(someUser);
        repository.GetToken(Arg.Is(someUserId), Arg.Is(someRefreshToken)).Returns(someUserToken);
        tokenReader.GetSidFromExpiredToken(Arg.Is(someAccessToken)).Returns(someUserId.ToString());

        _ = await refresher.GetNewAccessTokenAsync(someAccessToken, someRefreshToken);

        tokenGenerator.Received(1).GenerateAccessToken(Arg.Is(someUserId.ToString()),
            Arg.Is(someUser.UserName),
            Arg.Is(SecurityRoles.User),
            Arg.Is<CustomClaims[]>(claims => claims[0].Type == AdditionalClaimsGenerator.DeviceIdType && claims[0].Value == someDeviceId.ToString()));
    }


    [Test]
    public async Task UserTokenWithoutDeviceId_WhenRefreshing_ShouldSetDeviceId()
    {
        var someAccessToken = "alsdkj";
        var someRefreshToken = "qweru";
        var someUserId = Guid.NewGuid();
        var someUserToken = new UserToken { ForIssuer = null, AccessToken = someAccessToken, RefreshToken = someRefreshToken, IdUser = someUserId };
        var someUser = new User { Id = someUserId, UserName = "Papouche128" };
        var someDeviceId = Guid.NewGuid();
        deviceManager.GetCurrentDeviceIdAsync(Arg.Is(someUserId)).Returns(someDeviceId);
        repository.GetEntityById(Arg.Is(someUserId)).Returns(someUser);
        repository.GetToken(Arg.Is(someUserId), Arg.Is(someRefreshToken)).Returns(someUserToken);
        tokenReader.GetSidFromExpiredToken(Arg.Is(someAccessToken)).Returns(someUserId.ToString());

        _ = await refresher.GetNewAccessTokenAsync(someAccessToken, someRefreshToken);

        someUserToken.SpecificDeviceId.Should().Be(someDeviceId);
        tokenGenerator.Received(1).GenerateAccessToken(Arg.Is(someUserId.ToString()),
            Arg.Is(someUser.UserName),
            Arg.Is(SecurityRoles.User),
            Arg.Is<CustomClaims[]>(claims => claims[0].Type == AdditionalClaimsGenerator.DeviceIdType && claims[0].Value == someDeviceId.ToString()));
    }
}