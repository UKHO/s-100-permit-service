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
        /// 840025 - Get product key from Product Key Service started
        /// </summary>
        GetProductKeysRequestStarted = 840025,

        /// <summary>
        /// 840026 - Get product key from Product Key Service completed
        /// </summary>
        GetProductKeysRequestCompleted = 840026,

        /// <summary>
        /// 840027 - Exception occurred while get product key from Product Key Service
        /// </summary>
        GetProductKeysRequestFailed = 840027,

        /// <summary>
        /// 840028 - Manufacturer Id not found for Manufacturer keys in Memory Key Vault.
        /// </summary>
        ManufacturerIdNotFoundInKeyVault = 840028,

        /// <summary>
        /// 840029 -  Manufacturer Key found in Cache.
        /// </summary>
        ManufacturerKeyFoundInCache = 840029,

        /// <summary>
        /// 840030 - New Manufacturer Key is added in Cache.
        /// </summary>
        AddingNewManufacturerKeyInCache = 840030,

        /// <summary>
        /// 840031 - Access token is empty or null
        /// </summary>
        MissingAccessToken = 840031,

        /// <summary>
        /// 840032 - Request for retrying holdings api endpoint
        /// </summary>
        RetryHttpClientHoldingsRequest = 840032,

        /// <summary>
        /// 840033 - Request for retrying user permit api endpoint
        /// </summary>
        RetryHttpClientUserPermitRequest = 840033,

        /// <summary>
        /// 840034 - Request for retrying product key service api endpoint
        /// </summary>
        RetryHttpClientProductKeyServiceRequest = 840034,

        /// <summary>
        /// 840035 - Request to UserPermitService GetUserPermit endpoint completed with no content
        /// </summary>
        UserPermitServiceGetUserPermitsRequestCompletedWithNoContent = 840035,

        /// <summary>
        /// 840036 - Request to Holdings service GetHoldings completed with no content
        /// </summary>
        HoldingsServiceGetHoldingsRequestCompletedWithNoContent = 840036,

        /// <summary>
        /// 840037 - Expected hex string length not found
        /// </summary>
        HexStringLengthError = 840037,

        /// <summary>
        /// 840038 - Expected hex key length not found
        /// </summary>
        HexKeyLengthError = 840038,

        /// <summary>
        /// 840039 - Get decrypted keys from product keys started
        /// </summary>
        GetDecryptedKeysFromProductKeysStarted = 840039,

        /// <summary>
        /// 840040 - Get decrypted keys from product keys completed
        /// </summary>
        GetDecryptedKeysFromProductKeysCompleted = 840040,

        /// <summary>
        /// 840041 - Aes encryption exception
        /// </summary>
        AesEncryptionException = 840041,

        /// <summary>
        /// 840042 - Get decrypted hardware id from user permit started
        /// </summary>
        GetDecryptedHardwareIdFromUserPermitStarted = 840042,

        /// <summary>
        /// 840043 - Get decrypted hardware id from user permit completed
        /// </summary>
        GetDecryptedHardwareIdFromUserPermitCompleted = 840043,

        /// <summary>
        /// 840044 - Upn length or checksum validation failed
        /// </summary>
        UpnLengthOrChecksumValidationFailed = 840044,

        /// <summary>
        /// 840045 - Invalid Permit Xml Schema is recieved
        /// </summary>
        InvalidPermitXmlSchema = 840045,

        /// <summary>
        /// 840046 - Get Product list started 
        /// </summary>
        GetProductListStarted = 840046,

        /// <summary>
        /// 840047 - Get Product list completed
        /// </summary>
        GetProductListCompleted = 840047
    }
}