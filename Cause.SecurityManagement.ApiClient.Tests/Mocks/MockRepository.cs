using Cauca.ApiClient.Services;

namespace Cauca.ApiClient.Tests.Mocks
{
    public class MockRepository : BaseService<MockConfiguration>
    {
        public MockRepository(MockConfiguration configuration) : base(configuration)
        {
        }
    }
}