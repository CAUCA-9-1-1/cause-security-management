using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// A user candidate for group membership. The client builds the full name itself from
    /// <see cref="FirstName"/> and <see cref="LastName"/>.
    /// </summary>
    public sealed record UserForGroupDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
