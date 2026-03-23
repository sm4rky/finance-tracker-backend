using finance_tracker_backend.Types;

namespace finance_tracker_backend.Services;

public interface IResendTemplateEmailSender
{
    Task<EmailSendOutcome> SendWithTemplateAsync(
        string to,
        string templateId,
        IReadOnlyDictionary<string, string>? variables,
        CancellationToken cancellationToken = default);
}
