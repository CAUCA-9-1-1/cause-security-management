using Cause.SecurityManagement.Interfaces;
using Cause.SecurityManagement.Interfaces.Services;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Cause.SecurityManagement.Services
{
    public class AdministratorUserGenerator<TUser>
        : IAdministratorUserGenerator
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;
        private readonly SecurityConfiguration configuration;

        public AdministratorUserGenerator(
            ISecurityContext<TUser> context,
            IOptions<SecurityConfiguration> options)
        {
            this.context = context;
            configuration = options.Value;
        }

        public void EnsureAdminIsCreated()
        {
            if (!context.Users.Any(user => user.UserName == "admin"))
            {
                var user = new TUser
                {
                    Email = "dev@cauca.ca",
                    FirstName = "Admin",
                    LastName = "Cauca",
                    UserName = "admin",
                    IsActive = true,
                    Password = new PasswordGenerator().EncodePassword("admincauca", configuration.PackageName)
                };
                context.Add(user);
                context.SaveChanges();
            }
        }
    }
}