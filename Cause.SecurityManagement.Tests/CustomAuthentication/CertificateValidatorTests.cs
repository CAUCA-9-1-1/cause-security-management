using Cause.SecurityManagement.Authentication.Certificate;
using Cause.SecurityManagement.Authentication.Exceptions;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests
{
	public class CertificateValidatorTests
    {
        private readonly ICertificateValidator certificateValidator;
        private readonly SecurityConfiguration securityConfiguration = new SecurityConfiguration
        {
            CertificateIssuer = "O=CAUCA"
        };

        public CertificateValidatorTests()
        {
            var configuration = Options.Create(securityConfiguration);
            certificateValidator = new CertificateValidator(configuration);
        }

        [Test]
        public void WithoutCertificate_WhenValidated_ShouldThrowsException()
        {
            var httpContext = new DefaultHttpContext();

            Assert.Throws<CertificateNotPresent>(() => certificateValidator.ValidateCertificate(httpContext.Request.Headers));
        }

        [Test]
        public void WithInvalidCertificate_WhenValidated_ShouldThrowsExceptionAsync()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("ssl-client-verify", "FAILED");

            Assert.Throws<CertificateNotValid>(() => certificateValidator.ValidateCertificate(httpContext.Request.Headers));
        }

        [Test]
        public void WithInvalidCertificateIssuer_WhenValidated_ShouldThrowsExceptionAsync()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("ssl-client-verify", "SUCCESS");
            httpContext.Request.Headers.Add("ssl-client-issuer-dn", $"test,O=no-valid");

            Assert.Throws<CertificateNotValid>(() => certificateValidator.ValidateCertificate(httpContext.Request.Headers));
        }

        [Test]
        public void WithInvalidCertificateSubject_WhenValidated_ShouldThrowsExceptionAsync()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("ssl-client-verify", "SUCCESS");
            httpContext.Request.Headers.Add("ssl-client-issuer-dn", "test,O=no-valid");
            httpContext.Request.Headers.Add("ssl-client-subject-dn", $"{securityConfiguration.CertificateIssuer}");

            Assert.Throws<CertificateNotValid>(() => certificateValidator.ValidateCertificate(httpContext.Request.Headers));
        }

        [Test]
        public void WithValidCertificate_WhenValidated_ShouldThrowsExceptionAsync()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("ssl-client-verify", "SUCCESS");
            httpContext.Request.Headers.Add("ssl-client-issuer-dn", $"test,{securityConfiguration.CertificateIssuer}");
            httpContext.Request.Headers.Add("ssl-client-subject-dn", $"CN=a,{securityConfiguration.CertificateIssuer}");

            Assert.DoesNotThrow(() => certificateValidator.ValidateCertificate(httpContext.Request.Headers));
        }
    }
}