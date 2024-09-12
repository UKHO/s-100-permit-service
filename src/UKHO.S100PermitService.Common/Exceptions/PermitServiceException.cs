using Microsoft.Extensions.Logging;

namespace UKHO.S100PermitService.Common.Exceptions
{
    public class PermitServiceException : Exception
    {
        public EventId EventId { get; set; }

        public PermitServiceException(EventId eventId) : base()
        {
            EventId = eventId;
        }
    }
}
