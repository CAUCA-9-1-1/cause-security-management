using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Cause.SecurityManagement.Core;

namespace Cause.SecurityManagement.Authentication.Antiforgery
{
    public class SqlXmlRepository<TUser> : IXmlRepository
        where TUser: User, new()
    {
        private readonly ISecurityContext<TUser> context;

        public SqlXmlRepository(ISecurityContext<TUser> context)
        {
            this.context = context;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return new ReadOnlyCollection<XElement>(context.DataProtectionXmlElements.Select(x => XElement.Parse(x.Xml)).ToList());
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            context.DataProtectionXmlElements.Add(
                new DataProtectionElement
                {
                    Xml = element.ToString(SaveOptions.DisableFormatting)
                }
            );

            context.SaveChanges();
        }
    }
}
