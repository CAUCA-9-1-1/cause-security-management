using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace Cause.SecurityManagement.Antiforgery
{
    public class SqlXmlRepository : IXmlRepository
    {
        private readonly ISecurityContext Context;

        public SqlXmlRepository(ISecurityContext context)
        {
            Context = context;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return new ReadOnlyCollection<XElement>(Context.DataProtectionXMLElements.Select(x => XElement.Parse(x.Xml)).ToList());
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            Context.DataProtectionXMLElements.Add(
                new DataProtectionElement
                {
                    Xml = element.ToString(SaveOptions.DisableFormatting)
                }
            );

            Context.SaveChanges();
        }
    }
}
