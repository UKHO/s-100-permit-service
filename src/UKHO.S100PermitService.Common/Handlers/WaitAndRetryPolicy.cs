using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net;
using UKHO.S100PermitService.Common.Configuration;
using UKHO.S100PermitService.Common.Events;

namespace UKHO.S100PermitService.Common.Handlers
{
    public class WaitAndRetryPolicy : IWaitAndRetryPolicy
    {
        private readonly IOptions<WaitAndRetryConfiguration> _waitAndRetryConfiguration;

        public WaitAndRetryPolicy(IOptions<WaitAndRetryConfiguration> waitAndRetryConfiguration)
        {
            _waitAndRetryConfiguration = waitAndRetryConfiguration ?? throw new ArgumentNullException(nameof(waitAndRetryConfiguration));
        }

        public RetryPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger, EventIds eventId)
        {
            var retryCount = int.Parse(_waitAndRetryConfiguration.Value.RetryCount);
            var sleepDuration = double.Parse(_waitAndRetryConfiguration.Value.SleepDurationInSeconds);

            return Policy.HandleResult<HttpResponseMessage>(res => res.StatusCode == HttpStatusCode.ServiceUnavailable ||
                             res.StatusCode == HttpStatusCode.TooManyRequests ||
                             res.StatusCode == HttpStatusCode.InternalServerError).WaitAndRetry(retryCount, _ => TimeSpan.FromSeconds(sleepDuration),
                             onRetry: (response, timespan, retryAttempt, context) =>
                             {
                                 var retryAfterHeader = response.Result.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "retry-after");
                                 var correlationId = response.Result.RequestMessage!.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "x-correlation-id");
                                 var retryAfter = 0;
                                 logger
                                 .LogInformation(eventId.ToEventId(), "Re-trying service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}.",
                                  response.Result.RequestMessage.RequestUri, timespan.Add(TimeSpan.FromMilliseconds(retryAfter)).TotalMilliseconds, retryAttempt, correlationId.Value, response.Result.StatusCode);
                             });
        }
    }
}
