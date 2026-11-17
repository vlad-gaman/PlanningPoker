using System.Net;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.Controller.Services;

/// <summary>The one definition of which bucket a client address falls in, shared by every limiter.</summary>
public static class ClientAddressPartition
{
    public static string KeyOf(IPAddress? client)
    {
        if (client is null)
        {
            return RateLimitingConstants.UnknownClient;
        }

        return (client.IsIPv4MappedToIPv6 ? client.MapToIPv4() : client).ToString();
    }
}
