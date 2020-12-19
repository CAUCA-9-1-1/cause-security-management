using Cauca.ApiClient.Services;

namespace Cauca.ApiClient.Tests.Mocks
{
    public class MockSecureRepository : BaseSecureService<MockConfiguration>
    {
        public MockSecureRepository(MockConfiguration configuration) : base(configuration)
        {
        }
    }
}