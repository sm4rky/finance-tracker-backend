namespace finance_tracker_backend.Middleware;

public sealed class PlaidItemRelinkRequiredException(string message) : Exception(message);
