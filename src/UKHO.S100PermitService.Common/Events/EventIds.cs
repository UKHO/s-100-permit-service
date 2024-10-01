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
        /// 840011 - Permit service exception.
        /// </summary>
        PermitServiceException = 840011,

        /// <summary>
        /// 840012 - Get access token to call external api started.
        /// </summary>
        GetAccessTokenStarted = 840012,

        /// <summary>
        /// 840013 - Cached access token to call external api found.
        /// </summary>
        CachedAccessTokenFound = 840013,

        /// <summary>
        /// 840014 - Get new access token to call external api started.
        /// </summary>
        GetNewAccessTokenStarted = 840014,

        /// <summary>
        /// 840015 - Get new access token to call external api completed.
        /// </summary>
        GetNewAccessTokenCompleted = 840015,

        /// <summary>
        /// 840016 - Get access token to call external api completed.
        /// </summary>
        GetAccessTokenCompleted = 840016,

        /// <summary>
        /// 840017 - Caching access token to call external api started .
        /// </summary>
        CachingExternalEndPointTokenStarted = 840017,

        /// <summary>
        /// 840018 - Caching access token to call external api completed.
        /// </summary>
        CachingExternalEndPointTokenCompleted = 840018,

        /// <summary>
        /// 840019 - Request to Holdings service GetHoldings started
        /// </summary>
        HoldingsServiceGetHoldingsRequestStarted = 840019,

        /// <summary>
        /// 840020 - Request to Holdings service GetHoldings completed
        /// </summary>
        HoldingsServiceGetHoldingsRequestCompleted = 840020,

        /// <summary>
        /// 840021 - Request to Holdings service GetHoldings failed
        /// </summary>
        HoldingsServiceGetHoldingsRequestFailed = 840021,

        /// <summary>
        /// 840022 - Request to UserPermitService GetUserPermit endpoint started
        /// </summary>
        UserPermitServiceGetUserPermitsRequestStarted = 840022,

        /// <summary>
        /// 840023 - Request to UserPermitService GetUserPermit endpoint completed
        /// </summary>
        UserPermitServiceGetUserPermitsRequestCompleted = 840023,

        /// <summary>
        /// 840024 - Request to UserPermitService GetUserPermit endpoint failed
        /// </summary>
        UserPermitServiceGetUserPermitsRequestFailed = 840024,

        /// <summary>
        /// 840025 - Get permit key from Product Key Service started
        /// </summary>
        ProductKeyServicePostPermitKeyRequestStarted = 840025,

        /// <summary>
        /// 840026 - Get permit key from Product Key Service completed
        /// </summary>
        ProductKeyServicePostPermitKeyRequestCompleted = 840026,

        /// <summary>
        /// 840027 - Exception occurred while get permit key from Product Key Service
        /// </summary>
        ProductKeyServicePostPermitKeyRequestFailed = 840027,

        /// <summary>
        /// 840028 - Manufacturer Id not found for Manufacturer keys in Memory Key Vault.
        /// </summary>
        ManufacturerIdNotFoundInKeyVault = 840028,

        /// <summary>
        /// 840029 - Caching of Manufacturer Key started.
        /// </summary>
        ManufacturerKeyCachingStart = 840029,

        /// <summary>
        /// 840030 - Caching of Manufacturer Key end.
        /// </summary>
        ManufacturerKeyCachingEnd = 840030
    }
}