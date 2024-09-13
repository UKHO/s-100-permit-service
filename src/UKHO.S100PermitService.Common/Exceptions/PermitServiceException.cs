using Microsoft.Extensions.Logging;

namespace UKHO.S100PermitService.Common.Exceptions
{
    public class PermitServiceException : Exception
    {
        public EventId EventId { get; set; }

        public object[] MessageArguments { get; }

        public PermitServiceException(EventId eventId, string message, params object[] messageArguments) : base(message)
        {
            EventId = eventId;
            MessageArguments = messageArguments ?? Array.Empty<object>();
        }
    }
}