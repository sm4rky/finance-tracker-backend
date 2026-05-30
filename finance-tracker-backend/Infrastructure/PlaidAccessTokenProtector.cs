using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace finance_tracker_backend.Infrastructure;

public sealed class PlaidAccessTokenProtector(IConfiguration configuration)
{
    // private const string Purpose = "moneyinsight.plaid.access_token.v1";
    // private IDataProtector Protector => dataProtectionProvider.CreateProtector(Purpose);
    private const string Prefix = "v1:";
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public string Protect(string plaintextAccessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextAccessToken);
        // return Protector.Protect(plaintextAccessToken);

        var plaintext = Encoding.UTF8.GetBytes(plaintextAccessToken);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(GetKey(), TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, NonceSizeBytes, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return Prefix + Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedPayload);
        // return Protector.Unprotect(protectedPayload);

        if (!protectedPayload.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("Unsupported Plaid access token encryption payload format.");

        var payload = Convert.FromBase64String(protectedPayload[Prefix.Length..]);
        if (payload.Length <= NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Invalid Plaid access token encryption payload.");

        var nonce = payload[..NonceSizeBytes];
        var tag = payload[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        var ciphertext = payload[(NonceSizeBytes + TagSizeBytes)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(GetKey(), TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetKey()
    {
        var configuredKey = configuration["TokenEncryption:Key"];
        if (string.IsNullOrWhiteSpace(configuredKey))
            throw new InvalidOperationException("TokenEncryption:Key is required for Plaid access token encryption.");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("TokenEncryption:Key must be a base64-encoded 32-byte key.", ex);
        }

        if (key.Length != KeySizeBytes)
            throw new InvalidOperationException("TokenEncryption:Key must decode to exactly 32 bytes.");

        return key;
    }
}
