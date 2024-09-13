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
        /// 840004 - Create permit call started.
        /// </summary>
        CreatePermitStart = 840004,

        /// <summary>
        /// 840005 - Create permit call end.
        /// </summary>
        CreatePermitEnd = 840005,

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
        FileCreationEnd = 840009,

        /// <summary>
        /// 840010 - Empty permit xml is received.
        /// </summary>
        EmptyPermitXml = 840010,

        /// <summary>
        /// 840011 - Get access token to call external api started.
        /// </summary>
        GetAccessTokenStarted = 840011,

        /// <summary>
        /// 840012 - Cached access token to call external api found.
        /// </summary>
        CachedAccessTokenFound = 840012,

        /// <summary>
        /// 840013 - Get new access token to call external api started.
        /// </summary>
        GetNewAccessTokenStarted = 840013,

        /// <summary>
        /// 840014 - Get new access token to call external api completed.
        /// </summary>
        GetNewAccessTokenCompleted = 840014,

        /// <summary>
        /// 840015 - Get access token to call external api completed.
        /// </summary>
        GetAccessTokenCompleted = 840015,

        /// <summary>
        /// 840016 - Caching access token to call external api started .
        /// </summary>
        CachingExternalEndPointTokenStarted = 840016,

        /// <summary>
        /// 840017 - Caching access token to call external api completed.
        /// </summary>
        CachingExternalEndPointTokenCompleted = 840017,

        /// <summary>
        /// 840018 - Get holdings data to Holdings Service started.
        /// </summary>
        GetHoldingsDataToHoldingsService = 840018,

        /// <summary>
        /// 840019 - Request to get holdings data to Holdings Service completed.
        /// </summary>
        GetHoldingsDataToHoldingsCompleted = 840019,

        /// <summary>
        /// 840020 - Get holdings data to Holdings Service failed.
        /// </summary>
       GetHoldingsDataToHoldingsFailed = 840020,
    }
}