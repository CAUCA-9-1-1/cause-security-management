using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services;

public interface IDeviceManager
{
    Task<Guid> CreateNewDeviceAsync(Guid userId);
    Task<Guid> GetCurrentDeviceIdAsync(Guid userId);
}