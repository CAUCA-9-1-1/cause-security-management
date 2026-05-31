using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Core.Services;

public interface IDeviceManager
{
    Task<Guid> CreateNewDeviceAsync(Guid userId);
    Task<Guid> GetCurrentDeviceIdAsync(Guid userId);
}