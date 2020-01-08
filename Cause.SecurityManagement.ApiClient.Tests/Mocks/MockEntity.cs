using System;
using System.Collections.Generic;
using System.Text;

namespace Cause.SecurityManagement.ApiClient.Tests.Mocks
{
    public class MockEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = Guid.NewGuid().ToString();
    }
}
