using BlockedCountries.Business.Models.Responses;

namespace BlockedCountries.Business.Services;

public interface IBlockedAttemptService
{
    Task<ServiceResult<PagedResponse<BlockedAttemptResponse>>> GetBlockedAttemptsAsync(int page, int pageSize);
}


