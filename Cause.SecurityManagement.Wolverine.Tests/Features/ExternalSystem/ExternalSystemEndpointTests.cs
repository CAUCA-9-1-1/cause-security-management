using AwesomeAssertions;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Wolverine.Features.ExternalSystem;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using ExternalSystemModel = Cause.SecurityManagement.Models.ExternalSystem;
using ExternalSystemToken = Cause.SecurityManagement.Models.ExternalSystemToken;

namespace Cause.SecurityManagement.Wolverine.Tests.Features.ExternalSystem;

[TestFixture]
public class ExternalSystemLogonEndpointTests
{
    private IExternalSystemAuthenticationService svc = null!;

    [SetUp]
    public void SetUp() => svc = Substitute.For<IExternalSystemAuthenticationService>();

    [Test]
    public void WithValidApiKey_WhenLoggingIn_ShouldReturnLoginResult()
    {
        var token = new ExternalSystemToken { AccessToken = "at", RefreshToken = "rt", ExpiresOn = DateTime.UtcNow };
        var system = new ExternalSystemModel { Id = Guid.NewGuid(), Name = "SomeSystem" };
        svc.Login("key").Returns((token, system));

        var result = ExternalSystemLogonEndpoint.Handle(new ExternalSystemLoginInformations { Apikey = "key" }, svc);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<LoginResult>>()
            .Which.Value!.Name.Should().Be("SomeSystem");
    }

    [Test]
    public void WithInvalidApiKey_WhenLoggingIn_ShouldReturnUnauthorized()
    {
        svc.Login(Arg.Any<string>()).Returns(((ExternalSystemToken?)null, (ExternalSystemModel?)null));

        var result = ExternalSystemLogonEndpoint.Handle(new ExternalSystemLoginInformations { Apikey = "bad" }, svc);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }
}

[TestFixture]
public class ExternalSystemRefreshEndpointTests
{
    private IExternalSystemAuthenticationService svc = null!;
    private ILogger<ExternalSystemRefreshEndpoint> logger = null!;
    private DefaultHttpContext httpContext = null!;

    private readonly TokenRefreshResult tokens = new() { AccessToken = "at", RefreshToken = "rt" };

    [SetUp]
    public void SetUp()
    {
        svc = Substitute.For<IExternalSystemAuthenticationService>();
        logger = Substitute.For<ILogger<ExternalSystemRefreshEndpoint>>();
        httpContext = new DefaultHttpContext();
    }

    [Test]
    public async Task WithValidTokens_WhenRefreshing_ShouldReturnOk()
    {
        svc.RefreshAccessTokenAsync(tokens.AccessToken, tokens.RefreshToken).Returns("newAt");

        var result = await ExternalSystemRefreshEndpoint.HandleAsync(tokens, httpContext, svc, logger);

        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(200);
    }

    public static IEnumerable<TestCaseData> RefreshExceptions
    {
        get
        {
            yield return new TestCaseData(new InvalidTokenException("x", new Exception()), "Token-Invalid");
            yield return new TestCaseData(new SecurityTokenExpiredException(), "Refresh-Token-Expired");
            yield return new TestCaseData(new SecurityTokenException(), "Token-Invalid");
        }
    }

    [TestCaseSource(nameof(RefreshExceptions))]
    public async Task WithException_WhenRefreshing_ShouldReturnUnauthorizedAndSetHeader(Exception ex, string expectedHeader)
    {
        svc.RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<string>()).ThrowsAsync(ex);

        var result = await ExternalSystemRefreshEndpoint.HandleAsync(tokens, httpContext, svc, logger);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
        httpContext.Response.Headers.ContainsKey(expectedHeader).Should().BeTrue();
    }
}
