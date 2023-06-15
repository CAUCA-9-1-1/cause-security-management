using System.Linq;
using Cause.SecurityManagement.Authentication.Exceptions;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Authentication.Certificate
{
    public class CertificateValidator : ICertificateValidator
    {
        private readonly SecurityConfiguration configuration;
        private IHeaderDictionary headers;

        public CertificateValidator(IOptions<SecurityConfiguration> configuration)
        {
            this.configuration = configuration.Value;
        }

        public void ValidateCertificate(IHeaderDictionary headers)
        {
            this.headers = headers;
            ValidateAllCertificateInformation();
        }

        public string GetUserDn()
        {
            headers.TryGetValue("ssl-client-subject-dn", out var sslClientSubjectDn);

            return sslClientSubjectDn.ToString();
        }

        private void ValidateAllCertificateInformation()
        {
            ClientVerifyValidation();
            ClientIssuerDbValidation();
            ClientSubjectDbValidation();
        }

        private void ClientSubjectDbValidation()
        {
            headers.TryGetValue("ssl-client-subject-dn", out var sslClientSubjectDn);

            if (!sslClientSubjectDn.ToString().Contains("CN="))
            {
                throw new CertificateNotValid();
            }
        }

        private void ClientIssuerDbValidation()
        {
            headers.TryGetValue("ssl-client-issuer-dn", out var sslClientIssuerDn);

            if (configuration.CertificateIssuers == null && configuration.CertificateIssuers.Count == 0)
            {
                return;
            }
            if (!configuration.CertificateIssuers.Any(issuer => sslClientIssuerDn.ToString().EndsWith(issuer)))
            {
                throw new CertificateNotValid();
            }
        }

        private void ClientVerifyValidation()
        {
            headers.TryGetValue("ssl-client-verify", out var sslClientVerify);

            if (string.IsNullOrEmpty(sslClientVerify) || sslClientVerify.ToString() == "NONE")
            {
                throw new CertificateNotPresent();
            }
            else if (sslClientVerify.ToString() != "SUCCESS")
            {
                throw new CertificateNotValid();
            }
        }
    }
}
