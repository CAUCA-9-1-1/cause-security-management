using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects
{
    public class AuthentificationUserPermission
    {
        public Guid IdModulePermission { get; set; }
        public string Tag { get; set; }
		public bool IsAllowed { get; set; }
    }
}
