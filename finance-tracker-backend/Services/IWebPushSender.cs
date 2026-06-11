using finance_tracker_backend.Models;
using finance_tracker_backend.Types;

namespace finance_tracker_backend.Services;

public interface IWebPushSender
{
    Task<PushSendOutcome> SendAsync(
        PushSubscription subscription,
        string payloadJson,
        CancellationToken cancellationToken = default);
}
