using Cause.SecurityManagement.Authentication;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Cause.SecurityManagement
{
    public class AddAuthorizeFiltersControllerConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.IsDefined(typeof(OpenToExternalSystemWithCertificateAttribute), false))
            {
                controller.Filters.Add(new AuthorizeFilter("apicertificatepolicy"));
                controller.Filters.Add(new MultipleSchemeRequireUserAttribute());
            }
            else if (controller.ControllerType.IsDefined(typeof(OpenToExternalSystemAttribute), false))
            {
                controller.Filters.Add(new AuthorizeFilter("apipolicy"));
            }
            else
            {
                controller.Filters.Add(new AuthorizeFilter("defaultpolicy"));
            }
        }
    }
}