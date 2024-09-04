using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace UKHO.S100PermitService.Common.Enum
{
    /// <summary>
    /// Event Ids
    /// </summary>
    public enum EventIds
    {
        /// <summary>
        /// 940001 - An unhandled exception occurred while processing the request.
        /// </summary>
        UnhandledException = 940001,

        /// <summary>
        /// 940002 - Generate Permit API call started.
        /// </summary>
        GeneratePermitStarted = 940002,

        /// <summary>
        /// 940003 - Generate Permit API call end.
        /// </summary>
        GeneratePermitEnd = 940003,

        /// <summary>
        /// 940004 - Permit Mapping call started.
        /// </summary>
        PermitMapStart = 940004,

        /// <summary>
        /// 940005 - Permit Mapping call end.
        /// </summary>
        PermitMapEnd = 940005,

        /// <summary>
        /// 940006 - Xml serialization call started.
        /// </summary>
        XmlSerializationStart = 940006,

        /// <summary>
        /// 940007 - Xml serialization call end.
        /// </summary>
        XmlSerializationEnd = 940007,

        /// <summary>
        /// 940008 - File creation call started.
        /// </summary>
        FileCreationStart = 940008,

        /// <summary>
        /// 940009 - File creation call end.
        /// </summary>
        FileCreationEnd = 940009
    }

    /// <summary>
    /// EventId Extensions
    /// </summary>
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