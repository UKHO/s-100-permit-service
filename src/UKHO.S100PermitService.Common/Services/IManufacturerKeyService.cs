namespace UKHO.S100PermitService.Common.Services
{
    public interface IManufacturerKeyService
    {
        string GetManufacturerKeys(string secretName);
    }
}
