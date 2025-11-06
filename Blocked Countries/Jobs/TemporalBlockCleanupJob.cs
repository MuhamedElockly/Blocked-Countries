using BlockedCountries.Business.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlockedCountries.Api.Jobs;

public class TemporalBlockCleanupJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TemporalBlockCleanupJob> _logger;

    public TemporalBlockCleanupJob(IServiceProvider serviceProvider, ILogger<TemporalBlockCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Run()
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            var countryManagementService = scope.ServiceProvider.GetRequiredService<ICountryManagementService>();

            
            var cleanupMethod = countryManagementService
                .GetType()
                .GetMethod("CleanupExpiredTemporalBlocksAsync");

            if (cleanupMethod != null)
            {
                var task = (Task?)cleanupMethod.Invoke(countryManagementService, Array.Empty<object>());
                if (task != null)
                {
                    await task.ConfigureAwait(false);
                }
                _logger.LogInformation("Temporal block cleanup executed successfully.");
            }
            else
            {
                _logger.LogWarning("CleanupExpiredTemporalBlocksAsync method not found on ICountryManagementService implementation.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during temporal block cleanup job.");
            throw;
        }
    }
}


