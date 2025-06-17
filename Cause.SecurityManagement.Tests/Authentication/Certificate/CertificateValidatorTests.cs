using AwesomeAssertions;
using Cause.SecurityManagement.Authentication.Certificate;
using Cause.SecurityManagement.Authentication.Exceptions;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication.Certificate;

public class CertificateValidatorTests
{
    private readonly CertificateValidator certificateValidator;
    private readonly SecurityConfiguration securityConfiguration = new()
    {
        CertificateIssuers = ["O=CAUCA"]
    };

    public CertificateValidatorTests()
    {
        var configuration = Options.Create(securityConfiguration);
        certificateValidator = new CertificateValidator(Substitute.For<ILogger<CertificateValidator>>(), configuration);
    }

    [Test]
    public void WithoutCertificate_WhenValidated_ShouldThrowsException()
    {
        var httpContext = new DefaultHttpContext();

        var action = () => certificateValidator.ValidateCertificate(httpContext.Request.Headers);

        action.Should()
            .Throw<CertificateNotPresentException>();
    }

    [Test]
    public void WithInvalidCertificate_WhenValidated_ShouldThrowsExceptionAsync()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("ssl-client-verify", "FAILED");
        var action = () => certificateValidator.ValidateCertificate(httpContext.Request.Headers);

        action.Should()
            .Throw<CertificateNotValidException>()
            .WithMessage("ssl-client-verify is not 'SUCCESS'.  Received status is 'FAILED'.");
    }

    [Test]
    public void WithInvalidCertificateIssuer_WhenValidated_ShouldThrowsExceptionAsync()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("ssl-client-verify", "SUCCESS");
        httpContext.Request.Headers.Append("ssl-client-issuer-dn", "test,O=no-valid");

        var action = () => certificateValidator.ValidateCertificate(httpContext.Request.Headers);

        action.Should()
            .Throw<CertificateNotValidException>()
            .WithMessage("ssl_client_issuer-dn is not one of the allowed issuer.  Received issuer is 'test,O=no-valid'.");
    }

    [Test]
    public void WithInvalidCertificateSubject_WhenValidated_ShouldThrowsExceptionAsync()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("ssl-client-verify", "SUCCESS");
        httpContext.Request.Headers.Append("ssl-client-issuer-dn", "test,O=no-valid");
        httpContext.Request.Headers.Append("ssl-client-subject-dn", $"{securityConfiguration.CertificateIssuers[0]}");

        var action = () => certificateValidator.ValidateCertificate(httpContext.Request.Headers);

        action.Should()
            .Throw<CertificateNotValidException>()
            .WithMessage("ssl_client_issuer-dn is not one of the allowed issuer.  Received issuer is 'test,O=no-valid'.");
    }

    [Test]
    public void WithValidCertificate_WhenValidated_ShouldThrowsExceptionAsync()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("ssl-client-verify", "SUCCESS");
        httpContext.Request.Headers.Append("ssl-client-issuer-dn", $"test,{securityConfiguration.CertificateIssuers[0]}");
        httpContext.Request.Headers.Append("ssl-client-subject-dn", $"CN=a,{securityConfiguration.CertificateIssuers[0]}");

        var action = () => certificateValidator.ValidateCertificate(httpContext.Request.Headers);

        action.Should()
            .NotThrow<CertificateNotValidException>();
    }
}