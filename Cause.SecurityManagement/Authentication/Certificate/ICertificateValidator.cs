using Microsoft.AspNetCore.Http;

namespace Cause.SecurityManagement.Authentication.Certificate;

public interface ICertificateValidator
{
    void ValidateCertificate(IHeaderDictionary certificateHeaders);
    string GetUserDn();
}