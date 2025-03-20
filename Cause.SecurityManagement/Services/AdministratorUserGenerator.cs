using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Cause.SecurityManagement.Services;

public class AdministratorUserGenerator<TUser>(
    ISecurityContext<TUser> context,
    IOptions<SecurityConfiguration> options)
    : IAdministratorUserGenerator
    where TUser : User, new()
{
    private readonly SecurityConfiguration configuration = options.Value;

    public void EnsureAdminIsCreated()
    {
        if (!context.Users.Any(user => user.UserName == "admin"))
        {
            context.Add(GenerateAdminUser());
            context.SaveChanges();
        }
    }

    private TUser GenerateAdminUser()
    {
        return new TUser
        {
            Email = "dev@cauca.ca",
            FirstName = "Admin",
            LastName = "Cauca",
            UserName = "admin",
            IsActive = true,
            Password = new PasswordGenerator().EncodePassword("admincauca", configuration.PackageName)
        };
    }
}