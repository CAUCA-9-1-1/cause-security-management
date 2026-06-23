using System;
using System.Linq.Expressions;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public class DefaultUserAdditionalInformationProvider<TUser> : IUserAdditionalInformationProvider<TUser>
        where TUser : User, new()
    {
        public Expression<Func<TUser, string>> GetAdditionalInformation() => user => null;
    }
}
