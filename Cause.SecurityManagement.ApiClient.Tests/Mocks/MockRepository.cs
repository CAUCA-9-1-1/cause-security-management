using Cauca.ApiClient.Services;

namespace Cause.SecurityManagement.ApiClient.Tests.Mocks
{
    public class MockRepository : BaseService<MockConfiguration>
    {
        public MockRepository(MockConfiguration configuration) : base(configuration)
        {
        }
    }
}