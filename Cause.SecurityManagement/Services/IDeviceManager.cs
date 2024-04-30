using System;

namespace Cause.SecurityManagement.Services;

public interface IDeviceManager
{
    Guid CreateNewDevice(Guid userId);
}