using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using BlockedCountries.Business.Configuration;

namespace BlockedCountries.Api.Handlers;

public class GeolocationRateLimitHandler : DelegatingHandler
{
    private readonly int _maxPerMinute;
    private int _countInWindow;
    private DateTime _windowStartUtc;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public GeolocationRateLimitHandler(IOptions<GeolocationApiConfig> options)
    {
        _maxPerMinute = Math.Max(1, options.Value?.RateLimitPerMinute ?? 60);
        _windowStartUtc = DateTime.UtcNow;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Only throttle external geolocation calls (assumes base address points to provider)
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _windowStartUtc;
            if (elapsed >= TimeSpan.FromMinutes(1))
            {
                _windowStartUtc = now;
                _countInWindow = 0;
            }

            if (_countInWindow >= _maxPerMinute)
            {
                var delay = TimeSpan.FromMinutes(1) - elapsed;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    _windowStartUtc = DateTime.UtcNow;
                    _countInWindow = 0;
                }
            }

            _countInWindow++;
        }
        finally
        {
            _gate.Release();
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}


