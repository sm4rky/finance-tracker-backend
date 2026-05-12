namespace finance_tracker_backend.Services;

public interface IRecurringCashflowAdvanceService
{
    Task AdvancePredictedNextDatesAsync(CancellationToken cancellationToken = default);
}