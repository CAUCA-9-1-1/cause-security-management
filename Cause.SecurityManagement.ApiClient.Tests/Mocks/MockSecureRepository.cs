using Cauca.ApiClient.Services;

namespace Cause.SecurityManagement.ApiClient.Tests.Mocks
{
    public class MockSecureRepository : BaseSecureService<MockConfiguration>
    {
        public MockSecureRepository(MockConfiguration configuration) : base(configuration)
        {
        }
    }
}