using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IPushLogRepository
{
    Task<bool> ExistsByProfileAndDedupeKeyAsync(
        Guid profileId,
        string dedupeKey,
        CancellationToken cancellationToken = default);

    Task InsertAsync(PushLog log, CancellationToken cancellationToken = default);
}