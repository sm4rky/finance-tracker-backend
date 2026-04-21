namespace finance_tracker_backend.Repositories;

public interface IProfileMonthlyNetWorthRepository
{
    Task UpsertAsync(
        Guid profileId,
        DateOnly periodStartDate,
        decimal totalAssets,
        decimal totalLiabilities,
        decimal netWorth,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(DateOnly PeriodStartDate, decimal TotalAssets, decimal TotalLiabilities, decimal NetWorth, DateTimeOffset CreatedAt)>>
        ListByProfileIdAsync(
            Guid profileId,
            DateOnly? fromInclusive,
            DateOnly? toInclusive,
            CancellationToken cancellationToken = default);
}
