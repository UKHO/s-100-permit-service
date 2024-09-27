namespace UKHO.S100PermitService.Common.Services
{
    public interface IManufacturerKeyService
    {
        void CacheManufacturerKeys();
        string GetManufacturerKeys(string secretName);       
    }
}
