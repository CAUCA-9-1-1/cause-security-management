using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Services;

public static class AdditionalClaimsGenerator
{
    public static readonly string DeviceIdType = "deviceId";

    public static CustomClaims[] GetCustomClaims(Guid? specificDeviceId)
    {
        var additionalClaims = new List<CustomClaims>();
        if (specificDeviceId.HasValue)
        {
            additionalClaims.Add(new CustomClaims(DeviceIdType, specificDeviceId.Value.ToString()));
        }

        return additionalClaims.ToArray();
    }
}

public record struct CustomClaims(string Type, string Value);