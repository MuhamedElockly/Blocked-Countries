using BlockedCountries.Data.Models;

namespace BlockedCountries.Data.Repositories;

public interface IBlockedAttemptRepository
{
    Task AddAttemptAsync(BlockedAttempt attempt);
    Task<IEnumerable<BlockedAttempt>> GetAllAttemptsAsync();
}



