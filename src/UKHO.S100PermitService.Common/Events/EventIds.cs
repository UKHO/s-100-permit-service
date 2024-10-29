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
        /// 840002 - GeneratePermit API call started.
        /// </summary>
        GeneratePermitStarted = 840002,

        /// <summary>
        /// 840003 - GeneratePermit API call completed.
        /// </summary>
        GeneratePermitCompleted = 840003,

        /// <summary>
        /// 840004 - Process permit request started.
        /// </summary>
        ProcessPermitRequestStarted = 840004,

        /// <summary>
        /// 840005 - Process permit request completed.
        /// </summary>
        ProcessPermitRequestCompleted = 840005,

        /// <summary>
        /// 840006 - File creation call started.
        /// </summary>
        PermitXmlCreationStarted = 840006,

        /// <summary>
        /// 840007 - File creation call end.
        /// </summary>
        PermitXmlCreationCompleted = 840007,

        /// <summary>
        /// 840008 - Empty permit xml is received.
        /// </summary>
        EmptyPermitXml = 840008,

        /// <summary>
        /// 840009 - Permit service exception.
        /// </summary>
        PermitServiceException = 840009,

        /// <summary>
        /// 840010 - Get access token to call external api started.
        /// </summary>
        GetAccessTokenStarted = 840010,

        /// <summary>
        /// 840011 - Cached access token to call external api found.
        /// </summary>
        CachedAccessTokenFound = 840011,

        /// <summary>
        /// 840012 - Get new access token to call external api started.
        /// </summary>
        GetNewAccessTokenStarted = 840012,

        /// <summary>
        /// 840013 - Get new access token to call external api completed.
        /// </summary>
        GetNewAccessTokenCompleted = 840013,

        /// <summary>
        /// 840014 - Get access token to call external api completed.
        /// </summary>
        GetAccessTokenCompleted = 840014,

        /// <summary>
        /// 840015 - Caching access token to call external api started.
        /// </summary>
        CachingExternalEndPointTokenStarted = 840015,

        /// <summary>
        /// 840016 - Caching access token to call external api completed.
        /// </summary>
        CachingExternalEndPointTokenCompleted = 840016,

        /// <summary>
        /// 840017 - Request to Holdings service GetHoldings started.
        /// </summary>
        HoldingsServiceGetHoldingsRequestStarted = 840017,

        /// <summary>
        /// 840018 - Request to Holdings service GetHoldings completed.
        /// </summary>
        HoldingsServiceGetHoldingsRequestCompleted = 840018,

        /// <summary>
        /// 840019 - Request to Holdings service GetHoldings failed.
        /// </summary>
        HoldingsServiceGetHoldingsRequestFailed = 840019,

        /// <summary>
        /// 840020 - Request to UserPermitService GetUserPermit endpoint started.
        /// </summary>
        UserPermitServiceGetUserPermitsRequestStarted = 840020,

        /// <summary>
        /// 840021 - Request to UserPermitService GetUserPermit endpoint completed.
        /// </summary>
        UserPermitServiceGetUserPermitsRequestCompleted = 840021,

        /// <summary>
        /// 840022 - Request to UserPermitService GetUserPermit endpoint failed.
        /// </summary>
        UserPermitServiceGetUserPermitsRequestFailed = 840022,

        /// <summary>
        /// 840023 - Get product key from Product Key Service started.
        /// </summary>
        GetProductKeysRequestStarted = 840023,

        /// <summary>
        /// 840024 - Get product key from Product Key Service completed.
        /// </summary>
        GetProductKeysRequestCompleted = 840024,

        /// <summary>
        /// 840025 - Exception occurred while get product key from Product Key Service.
        /// </summary>
        GetProductKeysRequestFailed = 840025,

        /// <summary>
        /// 840026 - Manufacturer Id not found for Manufacturer keys in Memory Key Vault.
        /// </summary>
        ManufacturerIdNotFoundInKeyVault = 840026,

        /// <summary>
        /// 840027 - Filtered holdings total count before filtering and after filtering for highest expiry dates and removing duplicates.
        /// </summary>
        HoldingsFilteredCellCount = 840027,

        /// <summary>
        /// 840028 - New Manufacturer Key is added in Cache.
        /// </summary>
        AddingNewManufacturerKeyInCache = 840028,

        /// <summary>
        /// 840029 - Access token is empty or null.
        /// </summary>
        MissingAccessToken = 840029,

        /// <summary>
        /// 840030 - Request for retrying holdings api endpoint.
        /// </summary>
        RetryHttpClientHoldingsRequest = 840030,

        /// <summary>
        /// 840031 - Request for retrying user permit api endpoint.
        /// </summary>
        RetryHttpClientUserPermitRequest = 840031,

        /// <summary>
        /// 840032 - Request for retrying product key service api endpoint.
        /// </summary>
        RetryHttpClientProductKeyServiceRequest = 840032,

        /// <summary>
        /// 840033 - Request to UserPermitService GetUserPermit endpoint completed with no content.
        /// </summary>
        UserPermitServiceGetUserPermitsRequestCompletedWithNoContent = 840033,

        /// <summary>
        /// 840034 - Request to Holdings service GetHoldings completed with no content.
        /// </summary>
        HoldingsServiceGetHoldingsRequestCompletedWithNoContent = 840034,

        /// <summary>
        /// 840035 - Expected hex string length not found.
        /// </summary>
        HexStringLengthError = 840035,

        /// <summary>
        /// 840036 - Expected hex key length not found.
        /// </summary>
        HexKeyLengthError = 840036,

        /// <summary>
        /// 840037 - Decryption of product keys started.
        /// </summary>
        DecryptProductKeysStarted = 840037,

        /// <summary>
        /// 840038 - Decryption of product keys completed.
        /// </summary>
        DecryptProductKeysCompleted = 840038,

        /// <summary>
        /// 840039 - Aes encryption exception.
        /// </summary>
        AesEncryptionException = 840039,

        /// <summary>
        /// 840040 - Extraction of decrypted HW_ID from user permits started.
        /// </summary>
        ExtractDecryptedHardwareIdFromUserPermitStarted = 840040,

        /// <summary>
        /// 840041 - Extraction of decrypted HW_ID from user permits completed.
        /// </summary>
        ExtractDecryptedHardwareIdFromUserPermitCompleted = 840041,

        /// <summary>
        /// 840042 - Upn length or checksum validation failed.
        /// </summary>
        UpnLengthOrChecksumValidationFailed = 840042,

        /// <summary>
        /// 840043 - Invalid Permit Xml Schema is received.
        /// </summary>
        InvalidPermitXmlSchema = 840043,

        /// <summary>
        /// 840044 - Permit zip file creation completed.
        /// </summary>
        PermitZipCreationCompleted = 840044,

        /// <summary>
        /// 840045 - Manufacturer Key found in Cache.
        /// </summary>
        ManufacturerKeyFoundInCache = 840045,

        /// <summary>
        /// 840046 - Request to UserPermitService GetUserPermits responded licence not found
        /// </summary>
        UserPermitServiceGetUserPermitsLicenceNotFound = 840046,

        /// <summary>
        /// 840047 - Request to HoldingsService GetHoldings responded licence not found
        /// </summary>
        HoldingServiceGetHoldingsLicenceNotFound = 840047
    }
}