using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BlockedCountries.Data.Repositories;

namespace BlockedCountries.Business.Services;

public class TemporalBlockCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TemporalBlockCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public TemporalBlockCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TemporalBlockCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Temporal Block Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredBlocksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired temporal blocks");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Temporal Block Cleanup Service stopped");
    }

    private async Task CleanupExpiredBlocksAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var countryRepository = scope.ServiceProvider.GetRequiredService<ICountryRepository>();

        await countryRepository.RemoveExpiredTemporalBlocksAsync();
        _logger.LogDebug("Cleaned up expired temporal blocks");
    }
}

