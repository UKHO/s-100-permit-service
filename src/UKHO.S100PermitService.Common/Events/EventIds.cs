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
        /// 840008 - Permit service exception.
        /// </summary>
        PermitServiceException = 840008,

        /// <summary>
        /// 840009 - Get access token to call external api started.
        /// </summary>
        GetAccessTokenStarted = 840009,

        /// <summary>
        /// 840010 - Cached access token to call external api found.
        /// </summary>
        CachedAccessTokenFound = 840010,

        /// <summary>
        /// 840011 - Get new access token to call external api started.
        /// </summary>
        GetNewAccessTokenStarted = 840011,

        /// <summary>
        /// 840012 - Get new access token to call external api completed.
        /// </summary>
        GetNewAccessTokenCompleted = 840012,

        /// <summary>
        /// 840013 - Get access token to call external api completed.
        /// </summary>
        GetAccessTokenCompleted = 840013,

        /// <summary>
        /// 840014 - Caching access token to call external api started.
        /// </summary>
        CachingExternalEndPointTokenStarted = 840014,

        /// <summary>
        /// 840015 - Caching access token to call external api completed.
        /// </summary>
        CachingExternalEndPointTokenCompleted = 840015,

        /// <summary>
        /// 840016 - Get product key from Product Key Service completed with Ok response.
        /// </summary>
        GetProductKeysRequestCompletedWithStatus200Ok = 840016,

        /// <summary>
        /// 840017 - Get product key from Product Key Service started.
        /// </summary>
        GetProductKeysRequestStarted = 840017,

        /// <summary>
        /// 840018 - Exception occurred while get product key from Product Key Service.
        /// </summary>
        GetProductKeysRequestFailed = 840018,

        /// <summary>
        /// 840019 - Manufacturer Id not found for Manufacturer keys in Memory Key Vault.
        /// </summary>
        ManufacturerIdNotFoundInKeyVault = 840019,

        /// <summary>
        /// 840020 - New Manufacturer Key is added in Cache.
        /// </summary>
        AddingNewManufacturerKeyInCache = 840020,

        /// <summary>
        /// 840021 - Access token is empty or null.
        /// </summary>
        MissingAccessToken = 840021,

        /// <summary>
        /// 840022 - Request for retrying product key service api endpoint.
        /// </summary>
        RetryHttpClientProductKeyServiceRequest = 840022,

        /// <summary>
        /// 840023 - Expected hex string length not found.
        /// </summary>
        HexStringLengthError = 840023,

        /// <summary>
        /// 840024 - Expected hex key length not found.
        /// </summary>
        HexKeyLengthError = 840024,

        /// <summary>
        /// 840025 - Decryption of product keys started.
        /// </summary>
        DecryptProductKeysStarted = 840025,

        /// <summary>
        /// 840026 - Decryption of product keys completed.
        /// </summary>
        DecryptProductKeysCompleted = 840026,

        /// <summary>
        /// 840027 - Aes encryption exception.
        /// </summary>
        AesEncryptionException = 840027,

        /// <summary>
        /// 840028 - Extraction of decrypted HW_ID from user permits started.
        /// </summary>
        ExtractDecryptedHardwareIdFromUserPermitStarted = 840028,

        /// <summary>
        /// 840029 - Extraction of decrypted HW_ID from user permits completed.
        /// </summary>
        ExtractDecryptedHardwareIdFromUserPermitCompleted = 840029,

        /// <summary>
        /// 840030 - Invalid Permit Xml Schema is received.
        /// </summary>
        InvalidPermitXmlSchema = 840030,

        /// <summary>
        /// 840031 - Permit zip file creation completed.
        /// </summary>
        PermitZipCreationCompleted = 840031,

        /// <summary>
        /// 840032 - Manufacturer Key found in Cache.
        /// </summary>
        ManufacturerKeyFoundInCache = 840032,

        /// <summary>
        /// 840033 - Request to ProductKeyService GetProductKeys responded with status not found
        /// </summary>
        ProductKeyServiceGetProductKeysRequestCompletedWithStatus404NotFound = 840033,

        /// <summary>
        /// 840034 - Request to ProductKeyService GetProductKeys responded with status bad request
        /// </summary>
        ProductKeyServiceGetProductKeysRequestCompletedWithStatus400BadRequest = 840034,

        /// <summary>
        /// 840035 - Filtered products total count before filtering and after filtering for highest expiry dates and removing duplicates.
        /// </summary>
        ProductsFilteredCellCount = 840035,

        /// <summary>
        /// 840036 - Permit request validation failed.
        /// </summary>
        PermitRequestValidationFailed = 840036
    }
}