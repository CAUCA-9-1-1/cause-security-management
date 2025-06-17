using System.Linq;
using Cause.SecurityManagement.Authentication.Exceptions;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Authentication.Certificate
{
    public class CertificateValidator(
        ILogger<CertificateValidator> logger,
        IOptions<SecurityConfiguration> configuration) : ICertificateValidator
    {
        private readonly SecurityConfiguration configuration = configuration.Value;
        private IHeaderDictionary headers;

        public void ValidateCertificate(IHeaderDictionary certificateHeaders)
        {
            headers = certificateHeaders;
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
                var error = $"ssl-client-subject-dn header does not contains a CN. Current value is '{sslClientSubjectDn.ToString()}'.";
                logger.LogInformation("ssl-client-subject-dn header does not contains a CN. Current value is {SslclientSubjectDn}", sslClientSubjectDn.ToString());
                throw new CertificateNotValidException(error);
            }
        }

        private void ClientIssuerDbValidation()
        {
            headers.TryGetValue("ssl-client-issuer-dn", out var sslClientIssuerDn);

            if (configuration.CertificateIssuers == null || configuration.CertificateIssuers.Count == 0)
            {
                return;
            }
            if (!configuration.CertificateIssuers.Any(issuer => sslClientIssuerDn.ToString().EndsWith(issuer)))
            {
                var error = $"ssl_client_issuer-dn is not one of the allowed issuer.  Received issuer is '{sslClientIssuerDn.ToString()}'.";
                logger.LogInformation("ssl_client_issuer-dn is not one of the allowed issuer.  Received issuer is '{SslClientIssuerDn}'.", sslClientIssuerDn.ToString());
                throw new CertificateNotValidException(error);
            }
        }

        private void ClientVerifyValidation()
        {
            headers.TryGetValue("ssl-client-verify", out var sslClientVerify);

            if (string.IsNullOrEmpty(sslClientVerify) || sslClientVerify.ToString() == "NONE")
            {
                throw new CertificateNotPresentException();
            }
            else if (sslClientVerify.ToString() != "SUCCESS")
            {
                var error = $"ssl-client-verify is not 'SUCCESS'.  Received status is '{sslClientVerify.ToString()}'.";
                logger.LogInformation("ssl-client-verify is not 'SUCCESS'.  Received status is '{SslClientVerify}'.", sslClientVerify.ToString());
                throw new CertificateNotValidException(error);
            }
        }
    }
}
