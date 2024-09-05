namespace UKHO.S100PermitService.Common.Events
{
    /// <summary>
    /// Event Ids
    /// </summary>
    public enum EventIds 
    {
        /// <summary>
        /// 840001 - An unhandled exception occurred while processing the request.
        /// </summary>
        UnhandledException = 840001,

        /// <summary>
        /// 840002 - Generate Permit API call started.
        /// </summary>
        GeneratePermitStarted = 840002,

        /// <summary>
        /// 840003 - Generate Permit API call end.
        /// </summary>
        GeneratePermitEnd = 840003,

        /// <summary>
        /// 840004 - Permit Mapping call started.
        /// </summary>
        PermitMapStart = 840004,

        /// <summary>
        /// 840005 - Permit Mapping call end.
        /// </summary>
        PermitMapEnd = 840005,

        /// <summary>
        /// 840006 - Xml serialization call started.
        /// </summary>
        XmlSerializationStart = 840006,

        /// <summary>
        /// 840007 - Xml serialization call end.
        /// </summary>
        XmlSerializationEnd = 840007,

        /// <summary>
        /// 840008 - File creation call started.
        /// </summary>
        FileCreationStart = 840008,

        /// <summary>
        /// 840009 - File creation call end.
        /// </summary>
        FileCreationEnd = 840009
    }
}