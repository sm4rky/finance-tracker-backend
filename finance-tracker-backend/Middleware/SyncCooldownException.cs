namespace finance_tracker_backend.Middleware;

public sealed class SyncCooldownException(string message, TimeSpan retryAfter) : Exception(message)
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
