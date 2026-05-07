namespace finance_tracker_backend.Middleware;

public sealed class UsernameImmutableException(string message) : Exception(message);

public sealed class UsernameTakenException(string message) : Exception(message);
