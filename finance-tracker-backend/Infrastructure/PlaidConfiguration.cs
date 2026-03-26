using Microsoft.Extensions.Configuration;

namespace finance_tracker_backend.Infrastructure;

public static class PlaidConfiguration
{
    /// <summary>
    /// Resolves the Plaid secret for <c>Plaid:Environment</c>.
    /// Use <c>sandbox</c> + <c>Plaid:SandboxSecret</c> for Sandbox API, or <c>production</c> + <c>Plaid:ProductionSecret</c> for Production API.
    /// Limited production vs full production is determined by which credentials Plaid issued in the Dashboard, not a separate DevelopmentSecret.
    /// </summary>
    public static string ResolveSecret(IConfiguration configuration)
    {
        var environment = (configuration["Plaid:Environment"] ?? string.Empty).Trim().ToLowerInvariant();

        if (environment == "development")
            throw new InvalidOperationException(
                "Plaid:Environment 'development' is no longer supported. Use 'sandbox' with Plaid:SandboxSecret, or 'production' with Plaid:ProductionSecret.");

        var secret = environment switch
        {
            "sandbox" => configuration["Plaid:SandboxSecret"],
            "production" => configuration["Plaid:ProductionSecret"],
            _ => throw new InvalidOperationException(
                $"Plaid:Environment must be sandbox or production. Got: '{configuration["Plaid:Environment"]}'.")
        };

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                $"Plaid secret is not configured for environment '{configuration["Plaid:Environment"]}'. Set Plaid:SandboxSecret or Plaid:ProductionSecret.");

        return secret.Trim();
    }

    public static Going.Plaid.Environment MapApiEnvironment(string? plaidEnvironment)
    {
        var environment = (plaidEnvironment ?? string.Empty).Trim().ToLowerInvariant();

        if (environment == "development")
            throw new InvalidOperationException(
                "Plaid:Environment 'development' is no longer supported. Use 'sandbox' or 'production'.");

        return environment switch
        {
            "sandbox" => Going.Plaid.Environment.Sandbox,
            "production" => Going.Plaid.Environment.Production,
            _ => throw new InvalidOperationException(
                $"Plaid:Environment must be sandbox or production. Got: '{plaidEnvironment}'.")
        };
    }
}
