using Microsoft.Extensions.Logging;
using Polly.Retry;
using UKHO.S100PermitService.Common.Events;

namespace UKHO.S100PermitService.Common.Handlers
{
    public interface IWaitAndRetryPolicy
    {
        public RetryPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger, EventIds eventId);

    }
}
