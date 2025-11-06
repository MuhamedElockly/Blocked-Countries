using System.Net;

namespace BlockedCountries.Api.Middleware;

public class ClientIpMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClientIpMiddleware> _logger;

    public const string ClientIpKey = "ClientIpAddress";

    public ClientIpMiddleware(RequestDelegate next, ILogger<ClientIpMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = ExtractClientIpAddress(context);
        
        // Store IP address in HttpContext.Items for easy access throughout the request pipeline
        context.Items[ClientIpKey] = clientIp;
        
        // Also set it in HttpContext.Connection.RemoteIpAddress if not already set
        if (context.Connection.RemoteIpAddress == null && IPAddress.TryParse(clientIp, out var ipAddress))
        {
            context.Connection.RemoteIpAddress = ipAddress;
        }

        _logger.LogDebug("Client IP address extracted: {ClientIp}", clientIp);

        await _next(context);
    }

    private static string ExtractClientIpAddress(HttpContext context)
    {
        // For IIS direct connections (no proxy), these header checks will be skipped
        // and we'll use Connection.RemoteIpAddress (Priority 6) which IIS provides directly
        
        // Priority 1: Check X-Forwarded-For header (proxy/load balancer scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var ip in ips)
            {
                var trimmedIp = ip.Trim();
                if (IPAddress.TryParse(trimmedIp, out _))
                {
                    return trimmedIp;
                }
            }
        }

        // Priority 2: Check X-Real-IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            if (IPAddress.TryParse(realIp.Trim(), out _))
            {
                return realIp.Trim();
            }
        }

        // Priority 3: Check CF-Connecting-IP header (Cloudflare)
        var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cfConnectingIp))
        {
            if (IPAddress.TryParse(cfConnectingIp.Trim(), out _))
            {
                return cfConnectingIp.Trim();
            }
        }

        // Priority 4: Check X-Forwarded header
        var forwarded = context.Request.Headers["X-Forwarded"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var ips = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var ip in ips)
            {
                var trimmedIp = ip.Trim();
                if (IPAddress.TryParse(trimmedIp, out _))
                {
                    return trimmedIp;
                }
            }
        }

        // Priority 5: Check True-Client-IP header
        var trueClientIp = context.Request.Headers["True-Client-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(trueClientIp))
        {
            if (IPAddress.TryParse(trueClientIp.Trim(), out _))
            {
                return trueClientIp.Trim();
            }
        }

        // Priority 6: Use connection remote IP address (IIS direct connections use this)
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
