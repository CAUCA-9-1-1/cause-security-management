using Microsoft.AspNetCore.Http;

namespace Cause.SecurityManagement.Authentication;

public static class HeadersExtensions
{
    public static void AddOrUpdate(this IHeaderDictionary headers, string name, string value)
    {
        if (headers.ContainsKey(name))
        {
            headers[name] = value;
        }
        else
        {
            headers.Append(name, value);
        }
    }
}