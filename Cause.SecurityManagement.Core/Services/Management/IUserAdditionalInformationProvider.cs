using System;
using System.Linq.Expressions;
using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public interface IUserAdditionalInformationProvider<TUser> where TUser : User, new()
    {
        Expression<Func<TUser, string>> GetAdditionalInformation();
    }
}
