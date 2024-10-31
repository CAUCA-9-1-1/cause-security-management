using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cause.SecurityManagement;

public class UseDefaultAuthorizationWhenNotSpecifiedFilter(AuthorizationPolicy policy) : AuthorizeFilter(policy)
{
    public override Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // If there is another authorize filter, do nothing
        if (context.Filters.Any(item => item is IAsyncAuthorizationFilter && item != this))
        {
            return Task.FromResult(0);
        }

        //Otherwise apply this policy
        return base.OnAuthorizationAsync(context);
    }
}