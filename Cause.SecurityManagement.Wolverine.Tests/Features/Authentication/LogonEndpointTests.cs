using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Services;
using Cause.SecurityManagement.Wolverine.Features.Authentication;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Wolverine.Tests.Features.Authentication;

[TestFixture]
public class LogonEndpointTests
{
    private IEntityAuthenticator authenticator;

    private readonly LoginInformations loginInfo = new() { UserName = "user", Password = "pass" };

    [SetUp]
    public void SetUp()
    {
        authenticator = Substitute.For<IEntityAuthenticator>();
    }

    [Test]
    public async Task WithoutBodyAndHeader_WhenLogon_ShouldReturnUnauthorized()
    {
        var result = await LogonEndpoint.HandleAsync(null, null, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Test]
    public async Task WithNullResultFromAuthenticator_WhenLogon_ShouldReturnUnauthorized()
    {
        authenticator.LoginAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((LoginResult?)null);

        var result = await LogonEndpoint.HandleAsync(null, loginInfo, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Test]
    public async Task WithValidBody_WhenLogon_ShouldReturnOkWithLoginResult()
    {
        var expected = new LoginResult { AccessToken = "tok" };
        authenticator.LoginAsync(loginInfo.UserName, loginInfo.Password).Returns(expected);

        var result = await LogonEndpoint.HandleAsync(null, loginInfo, authenticator);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<LoginResult>>()
            .Which.Value.Should().Be(expected);
    }

    [Test]
    public async Task WithValidHeader_WhenLogon_ShouldDecodeAndCallAuthenticator()
    {
        var expected = new LoginResult { AccessToken = "tok" };
        authenticator.LoginAsync(loginInfo.UserName, loginInfo.Password).Returns(expected);
        var header = Convert.ToBase64String(Encoding.Default.GetBytes(Uri.EscapeDataString(JsonSerializer.Serialize(loginInfo))));

        var result = await LogonEndpoint.HandleAsync(header, null, authenticator);

        await authenticator.Received(1).LoginAsync(loginInfo.UserName, loginInfo.Password);
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<LoginResult>>();
    }
}
