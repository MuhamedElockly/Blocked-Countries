using Microsoft.Extensions.Logging;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Data.Repositories;

namespace BlockedCountries.Business.Services;

public class BlockedAttemptService : IBlockedAttemptService
{
    private readonly IBlockedAttemptRepository _attemptRepository;
    private readonly ILogger<BlockedAttemptService> _logger;

    public BlockedAttemptService(
        IBlockedAttemptRepository attemptRepository,
        ILogger<BlockedAttemptService> logger)
    {
        _attemptRepository = attemptRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResponse<BlockedAttemptResponse>>> GetBlockedAttemptsAsync(int page, int pageSize)
    {
        
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var allAttempts = await _attemptRepository.GetAllAttemptsAsync();
        var attempts = allAttempts
            .OrderByDescending(a => a.Timestamp)
            .ToList();

        var totalCount = attempts.Count;
        var pagedAttempts = attempts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new BlockedAttemptResponse
            {
                IpAddress = a.IpAddress,
                Timestamp = a.Timestamp,
                CountryCode = a.CountryCode,
                IsBlocked = a.IsBlocked,
                UserAgent = a.UserAgent
            })
            .ToList();

        return ServiceResult<PagedResponse<BlockedAttemptResponse>>.Success(new PagedResponse<BlockedAttemptResponse>
        {
            Items = pagedAttempts,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}

