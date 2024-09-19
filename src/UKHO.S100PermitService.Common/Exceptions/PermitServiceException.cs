using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Exceptions
{
    [ExcludeFromCodeCoverage]
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