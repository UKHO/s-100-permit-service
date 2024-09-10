using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    public class PermitServiceException : Exception
    {
        public EventId EventId { get; set; }

        public PermitServiceException(EventId eventId) : base()
        {
            EventId = eventId;
        }
    }
}
