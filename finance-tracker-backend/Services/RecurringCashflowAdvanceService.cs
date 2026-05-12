using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;
using Microsoft.Extensions.Configuration;

namespace finance_tracker_backend.Services;

/// <summary>Advances recurring rows whose <c>predicted_next_date</c> is on or before the UTC calendar day at execution time (catch-up for missed runs).</summary>
public sealed class RecurringCashflowAdvanceService(
    IProfileRecurringCashflowRepository recurringRepository,
    IConfiguration configuration,
    ILogger<RecurringCashflowAdvanceService> logger) : IRecurringCashflowAdvanceService
{
    public const int DefaultBatchSize = 200;

    private const int MaxAdvanceStepsPerRow = 5000;

    public async Task AdvancePredictedNextDatesAsync(CancellationToken cancellationToken = default)
    {
        var asOfUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        var batchSize = configuration.GetValue("Hangfire:RecurringCashflowAdvanceBatchSize", DefaultBatchSize);
        if (batchSize < 1)
            batchSize = DefaultBatchSize;

        var totalUpdated = 0;
        var totalSkipped = 0;
        var batches = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await recurringRepository
                .ListByCalendarDateAsync(asOfUtc, batchSize, cancellationToken)
                .ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            batches++;

            foreach (var row in batch)
            {
                var steps = 0;
                while (row.PredictedNextDate is { } due && due <= asOfUtc)
                {
                    steps++;
                    if (steps > MaxAdvanceStepsPerRow)
                    {
                        totalSkipped++;
                        logger.LogError(
                            "Stopped predicted-date advance for recurring row {Id} after {MaxSteps} steps (still due on or before {AsOf}). Check frequency/data.",
                            row.Id,
                            MaxAdvanceStepsPerRow,
                            asOfUtc);
                        break;
                    }

                    if (!RecurringCashflowPredictedDateAdvancement.TryAdvance(row, due, out var newLast, out var newPredicted))
                    {
                        totalSkipped++;
                        logger.LogWarning(
                            "Skipping predicted-date advance for recurring row {Id}: unsupported frequency {Frequency}.",
                            row.Id,
                            row.Frequency);
                        break;
                    }

                    row.LastDate = newLast;
                    row.PredictedNextDate = newPredicted;
                    row.UpdatedAt = DateTimeOffset.UtcNow;

                    await recurringRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
                    totalUpdated++;

                    if (newPredicted is null || newPredicted > asOfUtc)
                        break;
                }
            }

            if (batch.Count < batchSize)
                break;
        }

        logger.LogInformation(
            "Recurring cashflow predicted-date advance finished for UTC calendar date on or before {AsOfUtc}: {Updated} row updates, {Skipped} skips, {Batches} batch(es).",
            asOfUtc,
            totalUpdated,
            totalSkipped,
            batches);
    }
}
