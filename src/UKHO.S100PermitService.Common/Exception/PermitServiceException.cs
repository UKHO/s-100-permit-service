using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.S100PermitService.Common.Exception
{
    [ExcludeFromCodeCoverage]
    [Serializable()]
    public class PermitServiceException : IOException
    {
        public EventId EventId { get; set; }

        public PermitServiceException(EventId eventId) : base()
        {
            EventId = eventId;
        }
    }
}
