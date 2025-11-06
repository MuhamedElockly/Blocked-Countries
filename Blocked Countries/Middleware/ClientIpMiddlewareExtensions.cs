namespace BlockedCountries.Api.Middleware;

public static class ClientIpMiddlewareExtensions
{
    /// <summary>
    /// Extension method to add ClientIpMiddleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseClientIpMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ClientIpMiddleware>();
    }

    /// <summary>
    /// Extension method to easily get the client IP address from HttpContext
    /// </summary>
    public static string GetClientIpAddress(this HttpContext context)
    {
        // First check if middleware has set it in Items
        if (context.Items.TryGetValue(ClientIpMiddleware.ClientIpKey, out var ipObj) && ipObj is string ip)
        {
            return ip;
        }

        // Fallback to connection remote IP if middleware hasn't run
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            if (remoteIp.IsIPv4MappedToIPv6)
            {
                return remoteIp.MapToIPv4().ToString();
            }
            return remoteIp.ToString();
        }

        return "127.0.0.1";
    }
}
