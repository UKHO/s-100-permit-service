using Microsoft.Extensions.Logging;
using Polly;
using System.Net;
using UKHO.S100PermitService.Common.Events;

namespace UKHO.S100PermitService.Common.Handlers
{
    public class RetryPolicy
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger, EventIds eventId, int retryCount, double sleepDuration)
        {
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .OrResult(r => r.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(retryCount, (retryAttempt) =>
                {
                    return TimeSpan.FromSeconds(Math.Pow(sleepDuration, (retryAttempt - 1)));
                }, async (response, timespan, retryAttempt, context) =>
                {
                    var retryAfterHeader = response.Result.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "retry-after");
                    var correlationId = response.Result.RequestMessage!.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "x-correlation-id");
                    var retryAfter = 0;
                    if(response.Result.StatusCode == HttpStatusCode.TooManyRequests && retryAfterHeader.Value != null && retryAfterHeader.Value.Any())
                    {
                        retryAfter = int.Parse(retryAfterHeader.Value.First());
                        await Task.Delay(TimeSpan.FromMilliseconds(retryAfter));
                    }
                    logger
                    .LogInformation(eventId.ToEventId(), "Re-trying service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}.",
                        response.Result.RequestMessage.RequestUri, timespan.Add(TimeSpan.FromMilliseconds(retryAfter)).TotalMilliseconds, retryAttempt, correlationId.Value, response.Result.StatusCode);
                });
        }
    }
}
