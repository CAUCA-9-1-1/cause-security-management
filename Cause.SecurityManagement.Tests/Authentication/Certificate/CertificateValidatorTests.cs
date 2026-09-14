using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication.Certificate;
using Cause.SecurityManagement.Core.Authentication.Exceptions;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        certificateValidator = new CertificateValidator(new FakeLogger(), configuration);
    }

    // NUnit reuses a single instance of this fixture across every [Test] in it, so the shared
    // certificateValidator above (built once with its own throwaway logger) is fine for tests
    // that only check thrown exceptions - but each test below that inspects log entries builds
    // its own isolated CertificateValidator + FakeLogger, or entries from other tests would leak
    // into its assertions.
    private static CertificateValidator CreateValidator(SecurityConfiguration configuration, out FakeLogger logger)
    {
        logger = new FakeLogger();
        return new CertificateValidator(logger, Options.Create(configuration));
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

    [Test]
    public void WithInvalidCertificateSubjectMissingCn_WhenValidated_ShouldLogAtDebug()
    {
        var validator = CreateValidator(securityConfiguration, out var logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("ssl-client-verify", "SUCCESS");
        httpContext.Request.Headers.Append("ssl-client-issuer-dn", $"test,{securityConfiguration.CertificateIssuers[0]}");
        httpContext.Request.Headers.Append("ssl-client-subject-dn", "no-cn-here");

        var action = () => validator.ValidateCertificate(httpContext.Request.Headers);

        action.Should().Throw<CertificateNotValidException>();
        logger.Entries.Should().ContainSingle(entry =>
            entry.LogLevel == LogLevel.Debug &&
            entry.Message == "ssl-client-subject-dn header does not contains a CN. Current value is no-cn-here");
    }

    [Test]
    public void WithInvalidCertificateIssuer_WhenValidated_ShouldLogAtDebug()
    {
        var validator = CreateValidator(securityConfiguration, out var logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("ssl-client-verify", "SUCCESS");
        httpContext.Request.Headers.Append("ssl-client-issuer-dn", "test,O=no-valid");

        var action = () => validator.ValidateCertificate(httpContext.Request.Headers);

        action.Should().Throw<CertificateNotValidException>();
        logger.Entries.Should().ContainSingle(entry =>
            entry.LogLevel == LogLevel.Debug &&
            entry.Message == "ssl_client_issuer-dn is not one of the allowed issuer.  Received issuer is 'test,O=no-valid'.");
    }

    [Test]
    public void WithInvalidCertificateVerifyStatus_WhenValidated_ShouldLogAtDebug()
    {
        var validator = CreateValidator(securityConfiguration, out var logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("ssl-client-verify", "FAILED");

        var action = () => validator.ValidateCertificate(httpContext.Request.Headers);

        action.Should().Throw<CertificateNotValidException>();
        logger.Entries.Should().ContainSingle(entry =>
            entry.LogLevel == LogLevel.Debug &&
            entry.Message == "ssl-client-verify is not 'SUCCESS'.  Received status is 'FAILED'.");
    }

    [Test]
    public void WithoutCertificate_WhenValidated_ShouldLogNothing()
    {
        var validator = CreateValidator(securityConfiguration, out var logger);
        var httpContext = new DefaultHttpContext();

        var action = () => validator.ValidateCertificate(httpContext.Request.Headers);

        action.Should().Throw<CertificateNotPresentException>();
        logger.Entries.Should().BeEmpty(
            because: "ssl-client-verify NONE/empty happens on every unauthenticated request and must not log, even at Debug");
    }

    private sealed class FakeLogger : ILogger<CertificateValidator>
    {
        public List<(LogLevel LogLevel, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}