using System.Net.Http.Json;
using finance_tracker_backend.Types;
using Microsoft.Extensions.Configuration;

namespace finance_tracker_backend.Services;

public sealed class ResendTemplateEmailSender(HttpClient httpClient, IConfiguration configuration)
    : IResendTemplateEmailSender
{
    public async Task<EmailSendOutcome> SendWithTemplateAsync(
        string to,
        string templateId,
        IReadOnlyDictionary<string, string>? variables,
        CancellationToken cancellationToken = default)
    {
        var fromAddress = configuration["Resend:From"];
        if (string.IsNullOrWhiteSpace(fromAddress))
            return new EmailSendOutcome(false, null, "Resend:From is not configured.");

        if (string.IsNullOrWhiteSpace(templateId))
            return new EmailSendOutcome(false, null, "template id is empty.");

        var payload = new ResendTemplateEmailPayload(
            fromAddress,
            [to],
            new ResendTemplateReference(templateId.Trim(), variables));
        using var response = await httpClient
            .PostAsJsonAsync("emails", payload, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return new EmailSendOutcome(false, null, body);

        try
        {
            var ok = System.Text.Json.JsonSerializer.Deserialize<ResendEmailIdResponse>(body);
            return new EmailSendOutcome(true, ok?.Id, null);
        }
        catch
        {
            return new EmailSendOutcome(true, null, null);
        }
    }
}
