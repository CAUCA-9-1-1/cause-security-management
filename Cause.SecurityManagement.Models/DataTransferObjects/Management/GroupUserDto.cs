using System;

namespace Cause.SecurityManagement.Models.DataTransferObjects.Management
{
    /// <summary>
    /// A user belonging to a group. <see cref="Id"/> is the user identifier; on the read side
    /// the server composes <see cref="FullName"/> as "firstName lastName".
    /// </summary>
    public sealed record GroupUserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
    }
}
