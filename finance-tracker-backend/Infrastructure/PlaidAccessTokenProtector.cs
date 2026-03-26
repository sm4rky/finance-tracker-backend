using Microsoft.AspNetCore.DataProtection;

namespace finance_tracker_backend.Infrastructure;

public sealed class PlaidAccessTokenProtector(IDataProtectionProvider dataProtectionProvider)
{
    private const string Purpose = "moneyinsight.plaid.access_token.v1";

    private IDataProtector Protector => dataProtectionProvider.CreateProtector(Purpose);

    public string Protect(string plaintextAccessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextAccessToken);
        return Protector.Protect(plaintextAccessToken);
    }

    public string Unprotect(string protectedPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedPayload);
        return Protector.Unprotect(protectedPayload);
    }
}
