using System.Net;
using finance_tracker_backend.Models;
using finance_tracker_backend.Types;

namespace finance_tracker_backend.Services;

public sealed class WebPushSender(IConfiguration configuration) : IWebPushSender
{
    public async Task<PushSendOutcome> SendAsync(
        PushSubscription subscription,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        var publicKey = configuration["Vapid:PublicKey"];
        var privateKey = configuration["Vapid:PrivateKey"];
        var subject = configuration["Vapid:Subject"];

        if (string.IsNullOrWhiteSpace(publicKey) ||
            string.IsNullOrWhiteSpace(privateKey) ||
            string.IsNullOrWhiteSpace(subject))
            return new PushSendOutcome(false, "Vapid configuration is incomplete.", false);

        var webPushSubscription = new WebPush.PushSubscription(
            subscription.Endpoint,
            subscription.P256dh,
            subscription.Auth);
        var vapidDetails = new WebPush.VapidDetails(subject, publicKey, privateKey);

        try
        {
            var client = new WebPush.WebPushClient();
            await client
                .SendNotificationAsync(webPushSubscription, payloadJson, vapidDetails, cancellationToken)
                .ConfigureAwait(false);

            return new PushSendOutcome(true, null, false);
        }
        catch (WebPush.WebPushException ex)
        {
            var shouldDelete = ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;
            return new PushSendOutcome(false, ex.Message, shouldDelete);
        }
    }
}