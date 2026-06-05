using finance_tracker_backend.Enums;
namespace finance_tracker_backend.Types;
public sealed record TransactionsQuery
{
    public int Offset { get; init; }
    public int Limit { get; init; }
    public TransactionSortField SortBy { get; init; } = TransactionSortField.Date;
    public bool Descending { get; init; } = true;
    public IReadOnlyList<Guid> AccountIds { get; init; } = [];
    public bool IncludeUnlinkedTransactions { get; init; } = true;
    public IReadOnlyList<string> PfcPrimaryList { get; init; } = [];
    public bool IncludePfcUncategorized { get; init; }
    public IReadOnlyList<string> PaymentChannels { get; init; } = [];
    public bool? Pending { get; init; }
    public DateOnly? DateFromInclusive { get; init; }
    public DateOnly? DateToInclusive { get; init; }
    public decimal? AbsAmountMin { get; init; }
    public decimal? AbsAmountMax { get; init; }
    public TransactionFlow? AmountFlow { get; init; }
}
