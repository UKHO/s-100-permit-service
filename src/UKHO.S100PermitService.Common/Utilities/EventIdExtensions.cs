using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UKHO.S100PermitService.Common.Enum;

namespace UKHO.S100PermitService.Common.Utilities
{
    [ExcludeFromCodeCoverage]
    public static class EventIdExtensions
    {
        /// <summary>
        /// Event Id
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}
