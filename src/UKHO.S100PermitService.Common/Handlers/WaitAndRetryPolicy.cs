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
            ArgumentNullException.ThrowIfNull(waitAndRetryConfiguration, nameof(waitAndRetryConfiguration));
            _waitAndRetryConfiguration = waitAndRetryConfiguration ?? throw new ArgumentNullException(nameof(waitAndRetryConfiguration));
        }

        /// <summary>
        /// Retry if service responded with 429 TooManyRequests or 503 ServiceUnavailable StatusCodes.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="eventId">EventId</param>
        /// <returns>Service response message</returns>
        public AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicyAsync(ILogger logger, EventIds eventId)
        {
            var retryCount = int.Parse(_waitAndRetryConfiguration.Value.RetryCount);
            var sleepDuration = double.Parse(_waitAndRetryConfiguration.Value.SleepDurationInSeconds);

            return Policy.HandleResult<HttpResponseMessage>(res => res.StatusCode is HttpStatusCode.ServiceUnavailable or
                             HttpStatusCode.TooManyRequests).WaitAndRetryAsync(retryCount, _ => TimeSpan.FromSeconds(sleepDuration),
                             onRetry: (response, timespan, retryAttempt, context) =>
                             {
                                 var correlationId = response.Result.RequestMessage!.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == PermitServiceConstants.XCorrelationIdHeaderKey);
                                 var retryAfter = 0;
                                 logger
                                 .LogInformation(eventId.ToEventId(), "Re-trying service request for Uri: {RequestUri} with delay: {Delay}ms and retry attempt {Retry} with _X-Correlation-ID:{CorrelationId} as previous request was responded with {StatusCode}.",
                                  response.Result.RequestMessage.RequestUri, timespan.Add(TimeSpan.FromMilliseconds(retryAfter)).TotalMilliseconds, retryAttempt, correlationId.Value, response.Result.StatusCode);
                             });
        }
    }
}
