using System;
using Cause.SecurityManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection InjectSecurityServices(this IServiceCollection services, Action<DbContextOptionsBuilder> options)
		{
			services.AddTransient<AuthentificationService>();
			services.AddTransient<UserManagementService>();
			services.AddDbContext<ISecurityContext, SecurityContext>(options);
			return services;
		}        
    }
}