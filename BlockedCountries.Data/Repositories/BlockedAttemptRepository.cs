using System.Collections.Concurrent;
using BlockedCountries.Data.Models;

namespace BlockedCountries.Data.Repositories;

public class BlockedAttemptRepository : IBlockedAttemptRepository
{
    private readonly ConcurrentBag<BlockedAttempt> _attempts = new();

    public Task AddAttemptAsync(BlockedAttempt attempt)
    {
        _attempts.Add(attempt);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<BlockedAttempt>> GetAllAttemptsAsync()
    {
        return Task.FromResult<IEnumerable<BlockedAttempt>>(_attempts.ToList());
    }
}

