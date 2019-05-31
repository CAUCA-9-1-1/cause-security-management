using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement
{
	public static class MvcBuilderExtensions
	{
		public static IMvcBuilder InjectSecurityControllers(this IMvcBuilder builder)
		{
			// We're only using MvcBuildinderExtensions type to get this specific assembly to correctly inject
			// the controllers it contains.  We could have use any other class from Cause.SecurityManagement
			// or simply hardcode the assembly name.
			builder.AddApplicationPart(typeof(MvcBuilderExtensions).Assembly);
			return builder;
		}
	}
}