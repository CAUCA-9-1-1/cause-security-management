using System.Collections.Generic;

namespace Cause.SecurityManagement.Models
{
    public class ExternalSystem : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string CertificateSubjectDn { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public ICollection<ExternalSystemToken> Tokens { get; set; }
    }
}
