using finance_tracker_backend.Services;

namespace finance_tracker_backend.Jobs;

/// <summary>Hangfire recurring job: advances <c>last_date</c> / <c>predicted_next_date</c> when <c>predicted_next_date</c> is on or before the UTC calendar day at run time (Hangfire uses UTC).</summary>
public sealed class RecurringCashflowPredictedDateAdvanceJob(
    IServiceScopeFactory scopeFactory,
    ILogger<RecurringCashflowPredictedDateAdvanceJob> logger)
{
    public async Task RunAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRecurringCashflowAdvanceService>();
            await service.AdvancePredictedNextDatesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recurring cashflow predicted-date advance job failed.");
            throw;
        }
    }
}
