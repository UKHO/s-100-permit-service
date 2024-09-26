namespace UKHO.S100PermitService.Common.Services
{
    public interface IManufacturerKeyService
    {
        Task CacheManufacturerKeysAsync();
        Task<string> GetManufacturerKeysAsync(string secretName);       
    }
}
