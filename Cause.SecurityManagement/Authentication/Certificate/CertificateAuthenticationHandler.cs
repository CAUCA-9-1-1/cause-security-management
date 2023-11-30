using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Authentication.Exceptions;

namespace Cause.SecurityManagement.Authentication.Certificate
{
    public class CertificateAuthenticationHandler : AuthenticationHandler<CertificateAuthenticationOptions>
    {
        private readonly ICertificateValidator certificateValidator;
        private readonly IExternalSystemRepository repository;

        public CertificateAuthenticationHandler(
            IOptionsMonitor<CertificateAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder,
            ICertificateValidator certificateValidator,
            IExternalSystemRepository repository
        ) : base(options, logger, encoder)
        {
            this.certificateValidator = certificateValidator;
            this.repository = repository;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        { 
            try
            {
                certificateValidator.ValidateCertificate(Request.Headers);

                return Task.FromResult(AuthenticateResult.Success(GenerateTicket()));
            }
            catch (Exception)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }

        private AuthenticationTicket GenerateTicket()
        {
            var externalSystem = repository.GetByCertificateSubject(certificateValidator.GetUserDn());
            if (externalSystem == null)
            {
                throw new ExternalSystemNotFound();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Role, SecurityRoles.ExternalSystem),
                new Claim(ClaimTypes.Sid, externalSystem.Id.ToString()),
                new Claim(ClaimTypes.GivenName, externalSystem.Name),
            };

            var claimsIdentity = new ClaimsIdentity(claims, nameof(CertificateAuthenticationHandler));
            return new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), nameof(CertificateAuthenticationHandler));
        }
    }
}
