using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IEmailLogRepository
{
    Task InsertAsync(EmailLog log, CancellationToken cancellationToken = default);
}
