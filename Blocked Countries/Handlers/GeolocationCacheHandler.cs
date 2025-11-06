using Microsoft.Extensions.Caching.Memory;

namespace BlockedCountries.Api.Handlers;

public class GeolocationCacheHandler : DelegatingHandler
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;

    public GeolocationCacheHandler(IMemoryCache cache)
    {
        _cache = cache;
        _ttl = TimeSpan.FromMinutes(5);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Cache only GET requests
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var key = request.RequestUri?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (_cache.TryGetValue<(int StatusCode, string Content, string? MediaType)>(key, out var cached))
        {
            var response = new HttpResponseMessage((System.Net.HttpStatusCode)cached.StatusCode)
            {
                Content = new StringContent(cached.Content ?? string.Empty)
            };
            if (!string.IsNullOrEmpty(cached.MediaType))
            {
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(cached.MediaType);
            }
            return response;
        }

        var result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccessStatusCode && result.Content != null)
        {
            var content = await result.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var mediaType = result.Content.Headers.ContentType?.MediaType;

            // Store in cache
            _cache.Set(key, (StatusCode: (int)result.StatusCode, Content: content, MediaType: mediaType), _ttl);

            // Return a cloned response because content stream was consumed
            var clone = new HttpResponseMessage(result.StatusCode)
            {
                Content = new StringContent(content)
            };
            if (!string.IsNullOrEmpty(mediaType))
            {
                clone.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
            }
            foreach (var header in result.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            return clone;
        }

        return result;
    }
}


