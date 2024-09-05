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
        GeneratePermitEnd = 840003
    }
}