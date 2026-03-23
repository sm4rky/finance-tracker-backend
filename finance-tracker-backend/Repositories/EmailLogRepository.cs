using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class EmailLogRepository(Supabase.Client supabaseClient) : IEmailLogRepository
{
    public async Task InsertAsync(EmailLog log, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<EmailLog>().Insert(log, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
